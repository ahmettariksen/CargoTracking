using CargoTracking.API.Contracts.Couriers;
using CargoTracking.API.Contracts.Shipments;
using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
using CargoTracking.Application.Features.Shipments.Commands.CompleteShipmentReturn;
using CargoTracking.Application.Features.Shipments.Commands.DeliverShipment;
using CargoTracking.Application.Features.Shipments.Commands.PickupShipment;
using CargoTracking.Application.Features.Shipments.Commands.ReceiveShipmentAtWarehouse;
using CargoTracking.Application.Features.Shipments.Commands.TransferShipment;
using CargoTracking.Application.Features.Shipments.Commands.UpdateShipmentStatus;
using CargoTracking.Application.Features.Shipments.CreateShipments;
using CargoTracking.Application.Features.Shipments.Queries.GetShipmentTracking;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ShipmentsController : ControllerBase
{
    private readonly ISender _sender;

    public ShipmentsController(ISender sender)
    {
        _sender = sender;
    }

    // kargo oluşturma
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateShipmentCommand(
        request.ReceiverId,
        request.SenderAddressId,
        request.ReceiverAddressId,
        request.Weight,
        request.EstimatedDeliveryDate);

        var shipmentId = await _sender.Send(command, cancellationToken);

        return Ok(shipmentId);
    }

    // kargo statüsünü günceller
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateShipmentStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateShipmentStatusCommand(
        id,
        request.NewStatus,
        request.Description);

        await _sender.Send(command, cancellationToken);

        return Ok(new { Message = "Shipment status updated successfully." });
    }

    // kargo hareketlerini sorgulayan endpoint
    [HttpGet("track/{trackingNumber}")]
    [ProducesResponseType(typeof(ShipmentTrackingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentTrackingResponse>> Track(string trackingNumber, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetShipmentTrackingQuery(trackingNumber), cancellationToken);

        if (result is null)
        {
            return NotFound(new { Message = "Shipment was not found." });
        }

        return Ok(result);
    }

    //[HttpPost("{id:guid}/warehouse")]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //public async Task<IActionResult> ReceiveAtWarehouse(Guid id, CancellationToken cancellationToken)
    //{
    //    var command = new ReceiveShipmentAtWarehouseCommand(id);

    //    await _sender.Send(command, cancellationToken);

    //    return Ok(new
    //    {
    //        Message = "Shipment received at warehouse successfully."
    //    });
    //}

    // ilk kuryenin göndericiden kargonun alındığını kontrol eden endpoint
    [HttpPost("{shipmentId:guid}/pickup")]
    [Authorize(Roles = nameof(UserRole.Courier))]
    public async Task<IActionResult> PickupShipment(Guid shipmentId,CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
       new PickupShipmentCommand(shipmentId),
       cancellationToken);

        return Ok(new
        {
            ShipmentId = result,
            Message = "Shipment picked up from sender successfully."
        });
    }

    // target deposuna ulaşan kargoya 2.kurye atamasını yapan endpiont
    [HttpPost("{shipmentId:guid}/assign-delivery-courier")]
    [Authorize(Roles = nameof(UserRole.WarehouseEmployee))]
    public async Task<IActionResult> AutoAssignDeliveryCourier(
    Guid shipmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AutoAssignDeliveryCourierCommand(shipmentId), cancellationToken);

        return Ok(new
        {
            ShipmentId = shipmentId,
            DeliveryCourierId = result,
            Message = "Delivery courier assigned successfully."
        });
    }

    // ilk kuryeden gelen kargonun depoya ulaştığını kontrol eden enpoint.
    [HttpPost("{shipmentId:guid}/receive-at-warehouse")]
    [Authorize(Roles = nameof(UserRole.WarehouseEmployee))]
    public async Task<IActionResult> ReceiveAtWarehouse(
    Guid shipmentId,
    CancellationToken cancellationToken)
    {
        await _sender.Send(
            new ReceiveShipmentAtWarehouseCommand(shipmentId),
            cancellationToken);

        return Ok(new
        {
            ShipmentId = shipmentId,
            Message = "Shipment received at warehouse successfully."
        });
    }

    // ilk depodan hedef depoya transfer süreci başlatan endpoint.
    [HttpPost("{shipmentId:guid}/transfer")]
    [Authorize(Roles = nameof(UserRole.WarehouseEmployee))]
    public async Task<IActionResult> TransferShipment(
    Guid shipmentId,
    [FromBody] TransferShipmentRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new TransferShipmentCommand(
                shipmentId,
                request.TargetWarehouseId),
            cancellationToken);

        return Ok(new
        {
            ShipmentId = result,
            Message = "Shipment transfer started successfully."
        });
    }

    // 2.kuryenin hedef depodan kargoyu teslim aldığını kontrol eden enpointimiz
    [HttpPost("{shipmentId:guid}/pickup-for-delivery")]
    [Authorize(Roles = nameof(UserRole.Courier))]
    public async Task<IActionResult> PickupForDelivery(
    Guid shipmentId,
    CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new PickupShipmentForDeliveryCommand(shipmentId),
            cancellationToken);

        return Ok(new
        {
            ShipmentId = result,
            Message = "Shipment picked up from warehouse successfully."
        });
    }
    // kargo teslimi endpointi
    [HttpPost("{shipmentId:guid}/deliver")]
    [Authorize(Roles = nameof(UserRole.Courier))]
    public async Task<IActionResult> DeliverShipment(
    Guid shipmentId,
    CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeliverShipmentCommand(shipmentId),
            cancellationToken);

        return Ok(new
        {
            ShipmentId = result,
            Message = "Shipment delivered successfully."
        });
    }

    //alıcı yoksa 
    [HttpPost("{shipmentId:guid}/fail-delivery")]
    [Authorize(Roles = nameof(UserRole.Courier))]
    public async Task<IActionResult> FailDelivery(
    Guid shipmentId,
    [FromBody] FailDeliveryRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new FailDeliveryCommand(
                shipmentId,
                request.Reason),
            cancellationToken);

        return Ok(new
        {
            ShipmentId = result,
            Message = "Delivery failure recorded successfully."
        });
    }
    // İADE İŞ AKIŞI

//Başarısız teslimat
  //DeliveryFailed

//Depoya geri kabul
  //AtWarehouse

//İade kuryesi atanması
  //AssignedToCourier

//İade kuryesinin depodan alması
  //PickedUp

//Göndericiye teslim
  //Returned

    //İade kuryesi atama
    [HttpPost("{shipmentId:guid}/assign-return-courier")]
    [Authorize(Roles = nameof(UserRole.WarehouseEmployee))]
    public async Task<IActionResult> AssignReturnCourier(
    Guid shipmentId,
    CancellationToken cancellationToken)
    {
        var courierId = await _sender.Send(
            new AutoAssignReturnCourierCommand(shipmentId),
            cancellationToken);

        return Ok(new
        {
            Message = "Return courier assigned successfully.",
            CourierId = courierId
        });
    }

    //İade kuryesinin kargoyu depodan alması
    [HttpPost("{shipmentId:guid}/pickup-for-return")]
    [Authorize(Roles = nameof(UserRole.Courier))]
    public async Task<IActionResult> PickupForReturn(
    Guid shipmentId,
    CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new PickupShipmentForReturnCommand(shipmentId),
            cancellationToken);

        return Ok(new
        {
            Message = "Return shipment picked up successfully.",
            ShipmentId = result
        });
    }
    //İade teslimatını tamamlama
    [HttpPost("{shipmentId:guid}/complete-return")]
    [Authorize(Roles = nameof(UserRole.Courier))]
    public async Task<IActionResult> CompleteReturn(
    Guid shipmentId,
    CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CompleteShipmentReturnCommand(shipmentId),
            cancellationToken);

        return Ok(new
        {
            Message = "Shipment successfully returned to sender.",
            ShipmentId = result
        });
    }

}
