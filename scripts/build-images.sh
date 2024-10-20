#!/bin/bash

# Builds Docker images for each of the functions in this repo and pushes them
# to the wilsbxwcoshraue1acr ACR.
# **Run from the root of the repository**.

cd ./internal/functions/Willow.InspectionReport/Willow.InspectionReport.Function
dotnet restore
dotnet restore
cd -
docker build \
    -t wilsbxwcoshraue1acr.azurecr.io/willowplatform/inspectionreport:latest \
    -f internal/functions/Willow.InspectionReport/Willow.InspectionReport.Function/Dockerfile \
    .
docker push wilsbxwcoshraue1acr.azurecr.io/willowplatform/inspectionreport:latest

cd ./internal/functions/Willow.InspectionReport/Willow.InspectionReport.Function
dotnet restore
dotnet restore
cd -
docker build \
    -t wilsbxwcoshraue1acr.azurecr.io/willowplatform/inspectiongenerator:latest \
    -f ./internal/functions/Willow.InspectionGenerator/Willow.InspectionGenerator.Function/Dockerfile \
    .
docker push wilsbxwcoshraue1acr.azurecr.io/willowplatform/inspectiongenerator:latest

cd internal/functions/Willow.Tickets/Willow.Tickets.Function
dotnet restore
dotnet restore
cd -
docker build \
    -t wilsbxwcoshraue1acr.azurecr.io/willowplatform/ticket:latest \
    -f ./internal/functions/Willow.Tickets/Willow.Tickets.Function/Dockerfile \
    .
docker push wilsbxwcoshraue1acr.azurecr.io/willowplatform/ticket:latest

cd ./internal/functions/Willow.TicketTemplate/Willow.TicketTemplate.Function
dotnet restore
dotnet restore
cd -
docker build \
    -t wilsbxwcoshraue1acr.azurecr.io/willowplatform/tickettemplate:latest \
    -f ./internal/functions/Willow.TicketTemplate/Willow.TicketTemplate.Function/Dockerfile \
    .
docker push wilsbxwcoshraue1acr.azurecr.io/willowplatform/tickettemplate:latest

cd ./internal/functions/Willow.Communications/Willow.Communications.Function
dotnet restore
dotnet restore
cd -
docker build \
    -t wilsbxwcoshraue1acr.azurecr.io/willowplatform/commsvc:latest \
    -f ./internal/functions/Willow.Communications/Willow.Communications.Function/Dockerfile \
    .
docker push wilsbxwcoshraue1acr.azurecr.io/willowplatform/commsvc:latest
