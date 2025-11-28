namespace Application.DTOs
{
    public class GetFilteredAsyncDTO
    {
        public DateTime? fechaIniReserva { get; set; }
        public DateTime? fechaFinReserva { get; set; }
        public Guid? clienteId { get; set; }
        public Guid? espaciosCompartidosId { get; set; }
    }
}
