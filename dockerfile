FROM microsoft/aspnetcore-build
RUN dotnet --version
RUN mkdir /app
WORKDIR /app
COPY bin/publish .
ENTRYPOINT ["dotnet", "WebHook.Net.dll"]
