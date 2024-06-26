﻿using Bogus;
using Bogus.DataSets;
using Dataflow.Play.App.Models;

namespace Dataflow.Play.Sandbox;

public class FakeReceiptGenerationTests
{
    [Fact]
    public void OnlineReceipt()
    {
        var onlineReceiptFaker = new Faker<OnlineReceiptJsonContent>()
             .RuleFor(u => u.Id, f => f.Random.Uuid())
             .RuleFor(u => u.Products, f => Enumerable.Range(1, 3).Select(x => f.Vehicle.Model()).ToList())
             .RuleFor(u => u.CustomerName, f => f.Name.FirstName() + " " + f.Name.LastName())
             .RuleFor(u => u.Total, f => f.Random.Decimal(10, 1000));

        var cashierReceiptFaker = new Faker<CashierReceiptJsonContent>()
             .RuleFor(u => u.Id, f => f.Random.Uuid())
             .RuleFor(u => u.Products, f => Enumerable.Range(1, 3).Select(x => f.Vehicle.Model()).ToList())
             .RuleFor(u => u.Total, f => f.Random.Decimal(10, 1000));
        var online = onlineReceiptFaker.Generate();
    }
}
