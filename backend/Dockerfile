FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FTDNA.Services.FamilyTreeV3.sln .
COPY FTDNA.Services.FamilyTreeV3.Api/FTDNA.Services.FamilyTreeV3.Api.csproj FTDNA.Services.FamilyTreeV3.Api/
COPY FTDNA.Services.FamilyTreeV3.Common/FTDNA.Services.FamilyTreeV3.Common.csproj FTDNA.Services.FamilyTreeV3.Common/
COPY FTDNA.Services.FamilyTreeV3.Domain/FTDNA.Services.FamilyTreeV3.Domain.csproj FTDNA.Services.FamilyTreeV3.Domain/
COPY FTDNA.Services.FamilyTreeV3.Data.Internal/FTDNA.Services.FamilyTreeV3.Data.Internal.csproj FTDNA.Services.FamilyTreeV3.Data.Internal/
COPY FTDNA.Services.FamilyTreeV3.Data.External/FTDNA.Services.FamilyTreeV3.Data.External.csproj FTDNA.Services.FamilyTreeV3.Data.External/

RUN dotnet restore FTDNA.Services.FamilyTreeV3.sln

COPY . .

RUN dotnet publish FTDNA.Services.FamilyTreeV3.Api/FTDNA.Services.FamilyTreeV3.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "FTDNA.Services.FamilyTreeV3.Api.dll"]
