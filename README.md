<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://user-images.githubusercontent.com/79997186/183630747-3fc29619-2979-4349-81ac-cf51c10a1e90.png">
  <source media="(prefers-color-scheme: light)" srcset="https://user-images.githubusercontent.com/79997186/183532420-94ff0fa8-7250-424a-a989-d12cc98d6225.png">
  <img alt="header image">
</picture>

&nbsp;
&nbsp;
&nbsp;
&nbsp;
&nbsp;
&nbsp;

<picture width="50%">
  <source media="(prefers-color-scheme: dark)" srcset="https://user-images.githubusercontent.com/79997186/183628221-5dd0a8c4-00f3-4df7-ab4e-42ebd4ddd023.png" width="50">
  <source media="(prefers-color-scheme: light)" srcset="https://user-images.githubusercontent.com/79997186/183532688-0fe44ac9-a6b6-4a58-a10d-710116cd2e69.png" width="50">
  <img alt="introduction" width="50%">
</picture>

&nbsp;
&nbsp;
&nbsp;

QuantConnect enables you to run your algorithms in live mode with real-time market data. We have successfully hosted more than 200,000 live algorithms and have had more than $15B in volume traded on our servers since 2015. Brokerages supply a connection to the exchanges so that you can automate orders using LEAN. You can use multiple data feeds in live trading algorithms.

Interactive Brokers (IB) was founded by Thomas Peterffy in 1993 with the goal to “create technology to provide liquidity on better terms. Compete on price, speed, size, diversity of global products and advanced trading tools”. IB provides access to trading Equities, ETFs, Options, Futures, Future Options, Forex, Gold, Warrants, Bonds, and Mutual Funds for clients in over 200 countries and territories with no minimum deposit. IB also provides paper trading, a trading platform, and educational services.

&nbsp;
&nbsp;
&nbsp;

<picture width="50%">
  <source media="(prefers-color-scheme: dark)" srcset="https://user-images.githubusercontent.com/79997186/183628486-86463dad-292a-4d44-a1ba-d82ca72daf25.png" width="50">
  <source media="(prefers-color-scheme: light)" srcset="https://user-images.githubusercontent.com/79997186/183628548-aa16b16f-ecc2-48e3-9428-29b8d08461c4.png" width="50">
  <img alt="introduction" width="50%">
</picture>

&nbsp;
&nbsp;
&nbsp;

**You must have an available live trading node for each live trading algorithm you deploy.**

1. [Open the project](https://www.quantconnect.com) that you want to deploy.
2. Click the <img src="https://user-images.githubusercontent.com/79997186/183628750-db93a445-d4ae-4661-8019-0c1f5e21a698.png" width="28px">
 Deploy Live icon.

&nbsp;
&nbsp;
&nbsp;

```python
def Initialize(self) -> None:
    # Set the default order properties
    self.DefaultOrderProperties = InteractiveBrokersOrderProperties()
    self.DefaultOrderProperties.FaGroup = “TestGroupEQ”
    self.DefaultOrderProperties.FaMethod = “EqualQuantity”
    self.DefaultOrderProperties.FaProfile = “TestProfileP”
    self.DefaultOrderProperties.Account = “DU123456”

def OnData(self, slice: Slice) -> None:
    # Override the default order properties
    # “NetLiq” requires a order size input
    order_properties = InteractiveBrokersOrderProperties()
    order_properties.FaMethod = “NetLiq”
    self.LimitOrder(self.symbol, quantity, limit_price, orderProperties=order_properties)

    # “AvailableEquity” requires a order size input
    order_properties.FaMethod = “AvailableEquity”
    self.LimitOrder(self.symbol, quantity, limit_price, orderProperties=order_properties)

    # “PctChange” requires a percentage of portfolio input
    order_properties.FaMethod = “PctChange”
    self.SetHoldings(self.symbol, pct_portfolio, orderProperties=order_properties)
```

