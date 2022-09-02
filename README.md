![header-cheetah](https://user-images.githubusercontent.com/79997186/184224088-de4f3003-0c22-4a17-8cc7-b341b8e5b55d.png)

&nbsp;
&nbsp;
&nbsp;

## Introduction

This repository hosts the Atreyu Trading Brokerage Plugin Integration with the QuantConnect LEAN Algorithmic Trading Engine. LEAN is a brokerage agnostic operating system for quantitative finance. Thanks to open-source plugins such as this [LEAN](https://github.com/QuantConnect/Lean) can route strategies to almost any market.

[LEAN](https://github.com/QuantConnect/Lean) is maintained primarily by [QuantConnect](https://www.quantconnect.com), a US based technology company hosting a cloud algorithmic trading platform. QuantConnect has successfully hosted more than 200,000 live algorithms since 2015, and trades more than $1B volume per month.

### About Atreyu Trading

<p align="center">
<picture >
  <source media="(prefers-color-scheme: dark)" srcset="https://user-images.githubusercontent.com/79997186/184226065-f7a7679b-9800-4545-910f-c853931a8311.png">
  <source media="(prefers-color-scheme: light)" srcset="https://user-images.githubusercontent.com/79997186/184226246-7e327bc2-52f0-46c1-8902-8df5f212688c.png">
  <img alt="introduction" width="40%">
</picture>
<p>

[Atreyu Trading](https://qnt.co/atreyu) was founded by George Kledaras and John Pyrovolakis in 2015 with the goal to connect quantitative managers to US markets. [Atreyu Trading](https://qnt.co/atreyu) provides access to US prime brokers for clients from all countries that are not on the FinCen Anti-Money Laundering list. In addition to market access, [Atreyu Trading](https://qnt.co/atreyu) provides information about short availability. [Atreyu Trading](https://qnt.co/atreyu) doesn't have regulatory approval to trade for retail accounts, so you need to be a high net worth ($25M+) individual or firm to open an account. You need an organization on the Trading Firm or Institution tier to use the [Atreyu Trading](https://qnt.co/atreyu) brokerage.


## Using the Brokerage Plugin
  
### Deploying Atreyu with VSCode User Interace

  You can deploy using a visual interface in the QuantConnect Cloud. For more information see the [QuantConnect-Atreyu Integration Page](https://www.quantconnect.com/brokerages/atreyu).

  In the QuantConnect Cloud Platform you can harness the QuantConnect Live Data Feed. For most users this is substantially cheaper and easier than self-hosting.
  
  *Additionally, Atreyu Trading is sponsoring infrastructure costs for users of the QuantConnect cloud platform.*
  
### Deploying Atreyu with LEAN CLI

Follow these steps to start local live trading with the Atreyu brokerage:

1.  Open a terminal in your [CLI root directory](https://www.quantconnect.com/docs/v2/lean-cli/initialization/directory-structure#02-lean-init).
2.  Run lean live "`<projectName>`" to start a live deployment wizard for the project in ./`<projectName>` and then enter the brokerage number.

    ```
    $ lean live 'My Project'
     
    Select a brokerage:
    1. Paper Trading
    2. Interactive Brokers
    3. Tradier
    4. OANDA
    5. Bitfinex
    6. Coinbase Pro
    7. Binance
    8. Zerodha
    9. Samco
    10. Terminal Link
    11. Atreyu
    12. Trading Technologies
    13. Kraken
    14. FTX
    Enter an option: 
    ```
  
3.  Enter the number of the organization that has a subscription for the Atreyu module.

    ```
    $ lean live "My Project" 

    Select the organization with the Atreyu module subscription:
    1. Organization 1
    2. Organization 2
    3. Organization 3
       Enter an option: 1
    ```

4.  Enter the [Atreyu Trading](https://qnt.co/atreyu) server configuration.

    ```
    $ lean live "My Project"
    
    Host:
    Request port:
    Subscribe port: 
    ```

5.  Enter your [Atreyu Trading](https://qnt.co/atreyu) credentials.

    ``` 
    $ lean live "My Project"

    Username:
    Password:
    Client id: 
    ```

6.  Enter the broker MPID to use.

    ```
    $ lean live "My Project" 
    ```

Broker MPID:

7.  Enter the locate rqd to use.

    ``` 
    $ lean live "My Project"

    Locate rqd: 
    ```

8.  Enter the number of the data feed to use and then follow the steps required for the data connection.

    ``` 
    $ lean live 'My Project'

    Select a data feed:
    1. Interactive Brokers
    2. Tradier
    3. Oanda
    4. Bitfinex
    5. Coinbase Pro
    6. Binance
    7. Zerodha
    8. Samco
    9. Terminal Link
    10. Trading Technologies
    11. Kraken
    12. FTX
    13. IQFeed
    14. Polygon Data Feed
    15. Custom Data Only
  
        To enter multiple options, separate them with comma:
    ```

If you select IQFeed, see [IQFeed](https://www.quantconnect.com/docs/v2/lean-cli/live-trading/other-data-feeds/iqfeed) for set up instructions.  
If you select Polygon Data Feed, see [Polygon](https://www.quantconnect.com/docs/v2/lean-cli/live-trading/other-data-feeds/polygon) for set up instructions.

9. View the result in the `<projectName>`/live/`<timestamp>` directory. Results are stored in real-time in JSON format. You can save results to a different directory by providing the --output `<path>` option in step 2.

If you already have a live environment configured in your [Lean configuration file](https://www.quantconnect.com/docs/v2/lean-cli/initialization/configuration#03-Lean-Configuration), you can skip the interactive wizard by providing the --environment `<value>` option in step 2. The value of this option must be the name of an environment which has live-mode set to true.

## Account Types

[Atreyu Trading](https://qnt.co/atreyu) supports cash and margin accounts.

## Order Types and Asset Classes


[Atreyu Trading](https://qnt.co/atreyu) supports trading US Equities and the following order types:

- Market Order
- Limit Order
- Market-On-Close Order


## Downloading Data

For local deployment, the algorithm needs to download the following dataset:

[US Equities Security Master](https://www.quantconnect.com/datasets/quantconnect-us-equity-security-master) provided by QuantConnect  
[US Coarse Universe](https://www.quantconnect.com/datasets/quantconnect-us-coarse-universe-constituents)  
[Atreyu Trading](https://www.atreyutrading.com/) does not provide historical data.


## Brokerage Model

Lean models the brokerage behavior for backtesting purposes. The margin model is used in live trading to avoid placing orders that will be rejected due to insufficient buying power.

You can set the Brokerage Model with the following statements

    SetBrokerageModel(BrokerageName.Atreyu, AccountType.Cash);
    SetBrokerageModel(BrokerageName.Atreyu, AccountType.Margin);

[Read Documentation](https://www.quantconnect.com/docs/v2/our-platform/live-trading/brokerages/atreyu)

### Fees

We model the order fees of [Atreyu Trading](https://qnt.co/atreyu), which are $0.0035/share.

### Margin

We model buying power and margin calls to ensure your algorithm stays within the margin requirements.

[Read Documentation](https://www.quantconnect.com/docs/v2/our-platform/live-trading/brokerages/atreyu)

#### Buying Power

[Atreyu Trading](https://qnt.co/atreyu) allows up to 2x leverage for margin accounts.

#### Margin Calls

Regulation T margin rules apply. When the amount of margin remaining in your portfolio drops below 5% of the total portfolio value, you receive a [warning](https://www.quantconnect.com/docs/v2/writing-algorithms/reality-modeling/margin-calls#08-Monitor-Margin-Call-Events). When the amount of margin remaining in your portfolio drops to zero or goes negative, the portfolio sorts the generated margin call orders by their unrealized profit and executes each order synchronously until your portfolio is within the margin requirements.

### Slippage

Orders through [Atreyu Trading](https://qnt.co/atreyu) do not experience slippage in backtests. In live trading, your orders may experience slippage.

### Fills

We fill market orders immediately and completely in backtests. In live trading, if the quantity of your market orders exceeds the quantity available at the top of the order book, your orders are filled according to what is available in the order book.

### Short Availability

[Atreyu Trading](https://qnt.co/atreyu) provides short availability information through the AtreyuShortableProvider. If you try to short an Equity when there are no shares available to borrow, your order is invalidated.

### Settlements

If you trade with a margin account, trades settle immediately. If you trade with a cash account, Equity trades settle 3 days after the transaction date (T+3)

### Deposits and Withdraws

You can deposit and withdraw cash from your brokerage account while you run an algorithm that's connected to the account. We sync the algorithm's cash holdings with the cash holdings in your brokerage account every day at 7:45 AM Eastern Time (ET).

&nbsp;
&nbsp;
&nbsp;

![whats-lean](https://user-images.githubusercontent.com/79997186/184042682-2264a534-74f7-479e-9b88-72531661e35d.png)

&nbsp;
&nbsp;
&nbsp;

LEAN Engine is an open-source algorithmic trading engine built for easy strategy research, backtesting, and live trading. We integrate with common data providers and brokerages, so you can quickly deploy algorithmic trading strategies.

The core of the LEAN Engine is written in C#, but it operates seamlessly on Linux, Mac and Windows operating systems. To use it, you can write algorithms in Python 3.8 or C#. QuantConnect maintains the LEAN project and uses it to drive the web-based algorithmic trading platform on the website.

## Contributions

Contributions are warmly very welcomed but we ask you to read the existing code to see how it is formatted, commented and ensure contributions match the existing style. All code submissions must include accompanying tests. Please see the [contributor guide lines](https://github.com/QuantConnect/Lean/blob/master/CONTRIBUTING.md).

## Code of Conduct

We ask that our users adhere to the community [code of conduct](https://www.quantconnect.com/codeofconduct) to ensure QuantConnect remains a safe, healthy environment for
high quality quantitative trading discussions.

## License Model

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You
may obtain a copy of the License at

<http://www.apache.org/licenses/LICENSE-2.0>

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language
governing permissions and limitations under the License.
