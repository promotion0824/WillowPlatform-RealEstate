using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RealEstate.Customers.Settings;
using RealEstate.Customers.Stacks;
using Config = Pulumi.Config;

namespace RealEstate.Customers.Contexts;

public class EnvContext
{
    private readonly Customer[] _customers;

    public EnvContext()
    {
        var config = new Config();
        Environment = config.Require("environment");
        Region = config.Require("region");
        Tier = config.Require("tier");
        Zone = config.Require("zone");
        Adx = config.RequireObject<AdxSettings>("Adx");
        Adt = config.GetObject<AdtSettings>("Adt");
        DigitalTwinCore = config.RequireObject<WebAppSettings>("DigitalTwinCore");

        _customers = JsonSerializer.Deserialize<Customer[]>(File.ReadAllText($"customers.{Environment}.json"));
    }

    public IEnumerable<CustomerContext> Customers()
    {
        return _customers.Where(x => x.Region == Region).Select(customer => new CustomerContext(customer, this));
    }

    public string Environment { get; }

    public string Region { get; }

    public string Tier { get; }

    public string Zone { get; }

    public WebAppSettings DigitalTwinCore { get; set; }

    public AdxSettings Adx { get; }
    public AdtSettings Adt { get; }

    public class AdxSettings
    {
        public string ResourceGroup { get; set; }
        public string Cluster { get; set; }
        public string Location { get; set; }
        public string SubscriptionId { get; set; }
    }

    public class AdtSettings
    {
        public string SubscriptionId { get; set; }
    }
}