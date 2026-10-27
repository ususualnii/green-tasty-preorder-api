# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY GreenTasty.Preorder.Api.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 10000
ENTRYPOINT ["dotnet", "GreenTasty.Preorder.Api.dll"]
