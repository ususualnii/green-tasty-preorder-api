using Microsoft.AspNetCore.Mvc;

namespace GreenTasty.Preorder.Api.Controllers;

[ApiController]
[Route("group-reservations")]
public class GroupReservationsController(Store store) : ControllerBase
{
    /// <summary>
    /// Level 2 — consolidate the group pre-order: only submitted sessions, dishes merged by dishId,
    /// added to the owner's order, then order → SubmittedByCustomer and group → Submitted.
    /// </summary>
    [HttpPost("{groupReservationId}/submit")]
    [OwnerOnly]
    public ActionResult<List<DishQuantity>> Submit(string groupReservationId, SubmitRequest body)
    {
        var group = store.Groups.GetValueOrDefault(groupReservationId);
        if (group is null) return NotFound(new ErrorResponse($"Group reservation '{groupReservationId}' not found"));

        var caller = HttpContext.Items["user"] as User;
        if (caller?.Id != group.OwnerId)
            return StatusCode(403, new ErrorResponse("Only the owner of this group can submit it"));

        var reservation = store.Reservations.GetValueOrDefault(body.ReservationId);
        if (reservation is null) return NotFound(new ErrorResponse($"Reservation '{body.ReservationId}' not found"));

        var submitted = group.Sessions.Where(s => s.Status == SessionStatus.Submitted).ToList();
        if (submitted.Count == 0) return BadRequest(new ErrorResponse("There is no submitted session in this group"));

        // merge dishes from all submitted sessions by dishId
        var dishes = new Dictionary<string, int>();
        foreach (var item in submitted.SelectMany(s => s.Items))
            dishes[item.DishId] = dishes.GetValueOrDefault(item.DishId) + item.Quantity;

        if (dishes.Count == 0) return BadRequest(new ErrorResponse("Submitted sessions contain no dishes"));

        var order = reservation.Order;
        if (order is null) return BadRequest(new ErrorResponse($"Reservation '{body.ReservationId}' has no order"));

        foreach (var (dishId, quantity) in dishes)
        {
            var line = order.Items.FirstOrDefault(i => i.DishId == dishId);
            if (line is null) order.Items.Add(new SessionItem { DishId = dishId, Quantity = quantity });
            else line.Quantity += quantity;
        }

        order.Status = OrderStatus.SubmittedByCustomer;
        group.Status = GroupStatus.Submitted;

        return dishes.Select(kv => new DishQuantity(kv.Key, kv.Value)).ToList();
    }
}
