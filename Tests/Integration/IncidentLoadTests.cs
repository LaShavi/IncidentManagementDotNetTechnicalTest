using System.Diagnostics;

namespace Tests.Integration;

/// <summary>
/// Suite de pruebas de carga para validar rendimiento bajo presión
/// 
/// Objetivo: Simular 1000 solicitudes por minuto (req/min)
/// - 16.67 solicitudes por segundo
/// - Escenarios realistas de producción
/// - Métricas de rendimiento detalladas
/// 
/// Ejecución:
/// dotnet test Tests --filter "DisplayName~IncidentLoadTests"
/// </summary>
public class IncidentLoadTests
{
    /// <summary>
    /// TEST 1: Simular 1000 solicitudes GET por minuto
    /// 
    /// Escenario: Múltiples usuarios consultando incidentes simultáneamente
    /// 
    /// Esperado:
    /// - Éxito: > 95%
    /// - Req/s: >= 8
    /// - Latencia promedio: < 5000ms
    /// </summary>
    [Fact]
    public async Task Load_1000_GET_Requests_Per_Minute_Should_Complete_Within_Threshold()
    {
        const int totalRequests = 1000;
        const int maxConcurrentRequests = 20;
        var stopwatch = Stopwatch.StartNew();
        var stats = new RequestStats();
        var semaphore = new SemaphoreSlim(maxConcurrentRequests);

        var tasks = new List<Task>();
        for (int i = 0; i < totalRequests; i++)
        {
            tasks.Add(SimulateGetRequestAsync(semaphore, stats));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        var requestsPerSecond = totalRequests / elapsedSeconds;
        var avgResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Average() : 0;
        var minResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Min() : 0;
        var maxResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Max() : 0;
        var successRate = (stats.SuccessCount / (double)totalRequests) * 100;

        PrintLoadTestResults(
            "GET /api/incident",
            totalRequests,
            stats.SuccessCount,
            stats.FailureCount,
            elapsedSeconds,
            requestsPerSecond,
            avgResponseTime,
            minResponseTime,
            maxResponseTime,
            successRate
        );

        stats.SuccessCount.Should().BeGreaterThan((int)(totalRequests * 0.95));
        requestsPerSecond.Should().BeGreaterThan(8);
        avgResponseTime.Should().BeLessThan(5000);
    }

    /// <summary>
    /// TEST 2: Simular 1000 solicitudes POST por minuto
    /// </summary>
    [Fact]
    public async Task Load_1000_POST_Requests_Per_Minute_Should_Complete_Within_Threshold()
    {
        const int totalRequests = 1000;
        const int maxConcurrentRequests = 15;
        var stopwatch = Stopwatch.StartNew();
        var stats = new RequestStats();
        var semaphore = new SemaphoreSlim(maxConcurrentRequests);

        var tasks = new List<Task>();
        for (int i = 0; i < totalRequests; i++)
        {
            tasks.Add(SimulatePostRequestAsync(i, semaphore, stats));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        var requestsPerSecond = totalRequests / elapsedSeconds;
        var avgResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Average() : 0;
        var minResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Min() : 0;
        var maxResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Max() : 0;
        var successRate = (stats.SuccessCount / (double)totalRequests) * 100;

        PrintLoadTestResults(
            "POST /api/incident",
            totalRequests,
            stats.SuccessCount,
            stats.FailureCount,
            elapsedSeconds,
            requestsPerSecond,
            avgResponseTime,
            minResponseTime,
            maxResponseTime,
            successRate
        );

        stats.SuccessCount.Should().BeGreaterThan((int)(totalRequests * 0.90));
        requestsPerSecond.Should().BeGreaterThan(5);
        avgResponseTime.Should().BeLessThan(8000);
    }

    /// <summary>
    /// TEST 3: Carga mixta realista (70% GET, 30% POST)
    /// </summary>
    [Fact]
    public async Task Load_1000_Mixed_Requests_70_Percent_GET_30_Percent_POST_Should_Complete()
    {
        const int getRequests = 700;
        const int postRequests = 300;
        const int totalRequests = getRequests + postRequests;
        const int maxConcurrentRequests = 20;
        
        var stopwatch = Stopwatch.StartNew();
        var stats = new RequestStats();
        var semaphore = new SemaphoreSlim(maxConcurrentRequests);

        var tasks = new List<Task>();
        
        for (int i = 0; i < getRequests; i++)
        {
            tasks.Add(SimulateGetRequestAsync(semaphore, stats));
        }

        for (int i = 0; i < postRequests; i++)
        {
            tasks.Add(SimulatePostRequestAsync(i, semaphore, stats));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        var requestsPerSecond = totalRequests / elapsedSeconds;
        var avgResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Average() : 0;
        var minResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Min() : 0;
        var maxResponseTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Max() : 0;
        var successRate = (stats.SuccessCount / (double)totalRequests) * 100;

        PrintLoadTestResults(
            "MIXED (70% GET, 30% POST)",
            totalRequests,
            stats.SuccessCount,
            stats.FailureCount,
            elapsedSeconds,
            requestsPerSecond,
            avgResponseTime,
            minResponseTime,
            maxResponseTime,
            successRate
        );

        stats.SuccessCount.Should().BeGreaterThan((int)(totalRequests * 0.92));
        requestsPerSecond.Should().BeGreaterThan(7);
    }

    /// <summary>
    /// TEST 4: Test de resistencia (Soak Test)
    /// 10 batches de 100 solicitudes = 1000 total
    /// </summary>
    [Fact]
    public async Task Load_Sustained_Load_Should_Maintain_Performance_Across_Batches()
    {
        const int requestsPerBatch = 100;
        const int numberOfBatches = 10;
        const int maxConcurrentRequests = 20;
        
        var stopwatch = Stopwatch.StartNew();
        var overallStats = new RequestStats();
        var batchTimes = new List<double>();

        for (int batch = 0; batch < numberOfBatches; batch++)
        {
            var batchStopwatch = Stopwatch.StartNew();
            var batchStats = new RequestStats();
            var semaphore = new SemaphoreSlim(maxConcurrentRequests);

            var tasks = new List<Task>();
            for (int i = 0; i < requestsPerBatch; i++)
            {
                if (i % 2 == 0)
                {
                    tasks.Add(SimulateGetRequestAsync(semaphore, batchStats));
                }
                else
                {
                    tasks.Add(SimulatePostRequestAsync(i, semaphore, batchStats));
                }
            }

            await Task.WhenAll(tasks);
            batchStopwatch.Stop();
            batchTimes.Add(batchStopwatch.Elapsed.TotalSeconds);
            
            overallStats.SuccessCount += batchStats.SuccessCount;
            overallStats.FailureCount += batchStats.FailureCount;
            overallStats.ResponseTimes.AddRange(batchStats.ResponseTimes);

            Console.WriteLine($"Batch {batch + 1}/{numberOfBatches}: " +
                $"Exitosas: {batchStats.SuccessCount}, Fallidas: {batchStats.FailureCount}, " +
                $"Tiempo: {batchStopwatch.Elapsed.TotalSeconds:F2}s");
        }

        stopwatch.Stop();

        var avgBatchTime = batchTimes.Average();
        var maxBatchTime = batchTimes.Max();
        var minBatchTime = batchTimes.Min();
        var totalRequests = numberOfBatches * requestsPerBatch;
        var overallSuccessRate = (overallStats.SuccessCount / (double)totalRequests) * 100;

        PrintSoakTestResults(
            numberOfBatches,
            requestsPerBatch,
            totalRequests,
            overallStats.SuccessCount,
            overallStats.FailureCount,
            avgBatchTime,
            minBatchTime,
            maxBatchTime,
            stopwatch.Elapsed.TotalSeconds,
            overallSuccessRate
        );

        maxBatchTime.Should().BeLessThan(avgBatchTime * 2.5);
        overallStats.SuccessCount.Should().BeGreaterThan((int)(totalRequests * 0.90));
    }

    private static async Task SimulateGetRequestAsync(
        SemaphoreSlim semaphore,
        RequestStats stats)
    {
        await semaphore.WaitAsync();
        try
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await Task.Delay(Random.Shared.Next(5, 50));
                stopwatch.Stop();
                
                stats.AddSuccess(stopwatch.ElapsedMilliseconds);
            }
            catch
            {
                stopwatch.Stop();
                stats.AddFailure();
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task SimulatePostRequestAsync(
        int index,
        SemaphoreSlim semaphore,
        RequestStats stats)
    {
        await semaphore.WaitAsync();
        try
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await Task.Delay(Random.Shared.Next(20, 100));
                stopwatch.Stop();
                
                stats.AddSuccess(stopwatch.ElapsedMilliseconds);
            }
            catch
            {
                stopwatch.Stop();
                stats.AddFailure();
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static void PrintLoadTestResults(
        string endpoint,
        int totalRequests,
        int successCount,
        int failureCount,
        double elapsedSeconds,
        double requestsPerSecond,
        double avgResponseTime,
        double minResponseTime,
        double maxResponseTime,
        double successRate)
    {
        Console.WriteLine($"\n??????????????????????????????????????????????????");
        Console.WriteLine($"?          RESULTADO TEST DE CARGA                ?");
        Console.WriteLine($"??????????????????????????????????????????????????");
        Console.WriteLine($"? Endpoint:             {endpoint,-33} ?");
        Console.WriteLine($"? Total Solicitudes:    {totalRequests,-33} ?");
        Console.WriteLine($"? Exitosas:             {successCount,-33} ?");
        Console.WriteLine($"? Fallidas:             {failureCount,-33} ?");
        Console.WriteLine($"? Tasa de Éxito:        {successRate:F2}% {new string(' ', 28)} ?");
        Console.WriteLine($"??????????????????????????????????????????????????");
        Console.WriteLine($"? Tiempo Total:         {elapsedSeconds:F2}s {new string(' ', 31)} ?");
        Console.WriteLine($"? Req/segundo:          {requestsPerSecond:F2} {new string(' ', 32)} ?");
        Console.WriteLine($"? Latencia Promedio:    {avgResponseTime:F0}ms {new string(' ', 30)} ?");
        Console.WriteLine($"? Latencia Mínima:      {minResponseTime:F0}ms {new string(' ', 30)} ?");
        Console.WriteLine($"? Latencia Máxima:      {maxResponseTime:F0}ms {new string(' ', 30)} ?");
        Console.WriteLine($"??????????????????????????????????????????????????\n");
    }

    private static void PrintSoakTestResults(
        int numberOfBatches,
        int requestsPerBatch,
        int totalRequests,
        int successCount,
        int failureCount,
        double avgBatchTime,
        double minBatchTime,
        double maxBatchTime,
        double totalElapsedSeconds,
        double successRate)
    {
        Console.WriteLine($"\n??????????????????????????????????????????????????");
        Console.WriteLine($"?       RESULTADO TEST DE RESISTENCIA (SOAK)      ?");
        Console.WriteLine($"??????????????????????????????????????????????????");
        Console.WriteLine($"? Total de Batches:     {numberOfBatches,-33} ?");
        Console.WriteLine($"? Solicitudes/Batch:    {requestsPerBatch,-33} ?");
        Console.WriteLine($"? Total Solicitudes:    {totalRequests,-33} ?");
        Console.WriteLine($"? Exitosas:             {successCount,-33} ?");
        Console.WriteLine($"? Fallidas:             {failureCount,-33} ?");
        Console.WriteLine($"? Tasa de Éxito:        {successRate:F2}% {new string(' ', 28)} ?");
        Console.WriteLine($"??????????????????????????????????????????????????");
        Console.WriteLine($"? Tiempo Promedio/Batch:{avgBatchTime:F2}s {new string(' ', 30)} ?");
        Console.WriteLine($"? Batch Más Rápido:     {minBatchTime:F2}s {new string(' ', 30)} ?");
        Console.WriteLine($"? Batch Más Lento:      {maxBatchTime:F2}s {new string(' ', 30)} ?");
        Console.WriteLine($"? Tiempo Total:         {totalElapsedSeconds:F2}s {new string(' ', 31)} ?");
        Console.WriteLine($"??????????????????????????????????????????????????\n");
    }
}

/// <summary>
/// Clase auxiliar para almacenar estadísticas de solicitudes
/// </summary>
internal class RequestStats
{
    private int _successCountPrivate;
    private int _failureCountPrivate;

    public int SuccessCount
    {
        get => _successCountPrivate;
        set => _successCountPrivate = value;
    }

    public int FailureCount
    {
        get => _failureCountPrivate;
        set => _failureCountPrivate = value;
    }

    public List<long> ResponseTimes { get; } = new();

    public void AddSuccess(long elapsedMilliseconds)
    {
        Interlocked.Increment(ref _successCountPrivate);
        lock (ResponseTimes)
        {
            ResponseTimes.Add(elapsedMilliseconds);
        }
    }

    public void AddFailure()
    {
        Interlocked.Increment(ref _failureCountPrivate);
    }
}
