# Introduction
xUnit tests to trigger email notifications using the CommunicationsService. 

## Configuration and running
Prior to running the tests update the following settings in the `appsettings.json` file.

-`ServiceBusConnectionString`: The connection to service bus configured to recieve messages.<br/>
-`CustomerId`: The `id` for customer that has been configured to use deployed Willow platform.<br/>
-`UserId`: The `id` for user to whom emails are sent.<br/>