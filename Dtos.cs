namespace GreenTasty.Preorder.Api;

public record DishDto(string Id, string Name, int Price);
public record ItemDto(DishDto Dish, int Quantity);
public record ParticipantDto(string Id, string Type, string Name, List<ItemDto> Items, int Total, bool IsSubmitted);

/// <summary>Snapshot of a group: everyone who joined, per-person totals and the grand total.</summary>
public record GroupSnapshot(List<ParticipantDto> Participants, int Total);

public record GuestSessionIdResponse(string GuestSessionId);
public record SubmitRequest(string ReservationId);
public record DishQuantity(string DishId, int Quantity);
public record ErrorResponse(string Error);
