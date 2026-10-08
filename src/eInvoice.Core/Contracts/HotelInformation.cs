namespace eInvoice.Core.Contracts;

public class HotelInformation
{
    public string? FolioNum { get; set; }
    public string? RoomCode { get; set; }
    public string? ArrivalDate { get; set; }
    public string? DepartureDate { get; set; }

    public int? NumGuest { get; set; }

}
