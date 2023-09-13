# Stock-Challenge-CSharp - INOA
Stock Monitoring using C#, Visual Studio

<details>
<summary>How it works</summary>

The exe file needs 3 arguments: `stock name`, `value to sell`, `value to buy`<br>
The program uses two json files (that should be on `Publish/Configurations`) with data used for configure the email sender:
```
email-configuration.json:{
    {
        "serverSMTP": string,
        "serverPORT": number,
        "senderName": string,
        "senderEmail": string,
        "senderAppPassword": string,
        "emailRecipients":[
            {
                "name": string,
                "email": string
            },
            {
                "name": string,
                "email": string
            },
            ...
        ]
    }
}
```
```
email-templates.json:{
    "sellAlert":{
        "title": string,
        "body":  string
    },
    "buyAlert":{
        "title": string,
        "body":  string
    }
}
```
Obs:
- In `email-templates.json > sellAlert, buyAlert`<br>
- The title and body must be strings with parameters. 
- Title must have '{0}' as the stock name. 
- Body must have '{0}' as the stock name, '{1}' as the stock's current value, '{2}' as the template's limit value ('value to sell' on 'sellAlert' and 'value to buy' in 'buyAlert')
- Body can be written in HTML 
</details>

<details>
<summary>How to run</summary>

Enter folder `Publish` in terminal<br>
run `./stock-monitoring.exe <stock> <value to sell> <value to buy>`<br>
Ex: `./stock-monitoring.exe PETR4 23.65 22.48`

The program monitores the selected stock every minute, with delay between 15 - 30 minutes. That means that if you start the program at 13:30h, the initial monitored time can be between 13:00h and 13:15h, then the program monitores the following minutes<br>
Ex: Started the program at 13:30h. The initial time the stock was consulted was 13:11h. Then the next minute it will show the stock value on 13:12h, then 13:13h, and so on.
</details>