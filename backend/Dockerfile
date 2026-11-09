FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AprendizagemColaborativa.Api.csproj ./
RUN dotnet restore AprendizagemColaborativa.Api.csproj
COPY . ./
RUN dotnet publish AprendizagemColaborativa.Api.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "AprendizagemColaborativa.Api.dll"]
