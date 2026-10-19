using Itacr.Api.Tests.Catalog;
using Itacr.Application.Catalog.GetProducts;
using Itacr.Application.Common.Results;
using Itacr.Application.Orders;
using Itacr.Application.Orders.CreateOrder;
using Itacr.Application.Orders.CreatePickupOrder;
using Itacr.Domain.Customers;
using Itacr.Domain.Orders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Itacr.Api.Tests.Orders;

public sealed class CreatePickupOrderHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    private readonly SpyPickupRepository _repository = new();

    [Fact]
    public async Task HandleAsync_ShouldSaveTheTrimmedNameAndMenuPrices_WhenTheOrderIsValid()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command(name: "  Ana María  ", items: new CreateOrderItem(Soda, 3)), CancellationToken.None);

        // Assert
        Assert.True(result.Value.Created);
        var customer = Assert.Single(_repository.Customers);
        Assert.Equal(("Ana María", "7711234567"), (customer.Name, customer.Phone));
        var order = Assert.Single(_repository.Orders);
        Assert.Equal((OrderChannel.MobileApp, OrderStatus.New, 87m), (order.Channel, order.Status, order.Total));
        Assert.Null(order.WaiterId);
        Assert.Null(order.TableId);
        Assert.Equal(2, _repository.MaxActiveOrders);
        Assert.Equal(87m, result.Value.Order.Total);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotTouchTheRepository_WhenTheCommandIsInvalid()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command(phone: null), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(["Este campo es obligatorio."], result.Error!.Fields["phone"]);
        Assert.Empty(_repository.Orders);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheExistingOrderWithoutSaving_WhenTheClientOrderIdWasAlreadySent()
    {
        // Arrange
        var command = Command();
        _repository.Existing = new PickupOrder(command.ClientOrderId, 12, OrderStatus.Ready, 29m, Now.UtcDateTime, [new PickupOrderItem("Refresco de Cola", 1, 29m, 29m)]);
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Value.Created);
        Assert.Equal((12, OrderStatus.Ready), (result.Value.Order.OrderNumber, result.Value.Order.Status));
        Assert.Empty(_repository.Orders);
    }

    [Fact]
    public async Task HandleAsync_ShouldAnswerALimitConflictWithTheConfiguredNumber_WhenThePhoneHasTooManyActiveOrders()
    {
        // Arrange
        _repository.Outcome = PickupCreateOutcome.LimitReached;
        var handler = CreateHandler(maxActiveOrders: 3);

        // Act
        var result = await handler.HandleAsync(Command(), CancellationToken.None);

        // Assert
        Assert.Equal((ErrorKind.Conflict, OrdersErrors.PickupLimitType), (result.Error!.Kind, result.Error.Type));
        Assert.Contains("3 pedidos", result.Error!.Detail);
    }

    [Fact]
    public async Task HandleAsync_ShouldAnswerAConflict_WhenTheIdWasSavedAsAnotherKindOfOrder()
    {
        // Arrange
        _repository.Outcome = PickupCreateOutcome.AlreadyExists;
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command(), CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.PickupOrderIdTaken, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseAProductNotOnTheMenu_WhenAnIdIsUnknown()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(Command(items: new CreateOrderItem(Guid.NewGuid(), 1)), CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.UnknownProducts, result.Error);
        Assert.Empty(_repository.Orders);
    }

    private static CreatePickupOrderCommand Command(string? name = "Ana", string? phone = "7711234567", params CreateOrderItem[] items) =>
        new(Guid.NewGuid(), name, phone, items.Length == 0 ? [new CreateOrderItem(Soda, 1)] : items);

    private CreatePickupOrderHandler CreateHandler(int maxActiveOrders = 2)
    {
        var options = Options.Create(new PublicOrdersOptions { MaxActiveOrdersPerPhone = maxActiveOrders });
        var catalog = new FakeCatalogRepository(
            [],
            [new ProductResponse(Soda, Guid.NewGuid(), "Refresco de Cola", string.Empty, 29m, null, false)]);
        return new CreatePickupOrderHandler(_repository, catalog, new FakeTimeProvider(Now), options, new CreatePickupOrderValidator(options));
    }

    private sealed class SpyPickupRepository : IPickupOrderRepository
    {
        public List<Order> Orders { get; } = [];

        public List<Customer> Customers { get; } = [];

        public PickupOrder? Existing { get; set; }

        public PickupCreateOutcome Outcome { get; set; } = PickupCreateOutcome.Created;

        public int MaxActiveOrders { get; private set; }

        public Task<PickupOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken) => Task.FromResult(Existing);

        public Task<PickupCreateOutcome> CreateAsync(Order order, Customer customer, int maxActiveOrders, CancellationToken cancellationToken)
        {
            MaxActiveOrders = maxActiveOrders;
            if (Outcome == PickupCreateOutcome.Created)
            {
                order.OrderNumber = 7;
                Orders.Add(order);
                Customers.Add(customer);
            }

            return Task.FromResult(Outcome);
        }
    }
}
