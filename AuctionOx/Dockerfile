# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Cache dependencies
COPY ["AuctionOx.csproj", "./"]
RUN dotnet restore "AuctionOx.csproj"
# Copy source and publish
COPY . .
RUN dotnet publish "AuctionOx.csproj" -c Release -o /app/publish /p:UseAppHost=false
# Stage 2: Lean runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuctionOx.dll"]