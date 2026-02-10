FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Backend/ ./Backend/
RUN dotnet restore Backend/Hrevolve.Web/Hrevolve.Web.csproj
RUN dotnet publish Backend/Hrevolve.Web/Hrevolve.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","Hrevolve.Web.dll"]
