FROM microsoft/aspnetcore-build
RUN dotnet --version
RUN mkdir /app
RUN mkdir /app/sshkeys
WORKDIR /app
COPY bin/publish .
COPY sshkeys sshkeys
ENTRYPOINT ["dotnet", "WebHook.Net.dll", "--server.urls", "\"http://*:7777\""]
