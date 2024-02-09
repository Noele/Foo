using System.Reflection;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using DisCatSharp.Net;
using Foo.Helpers;
using Foo.Objects;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace Foo;

/// <summary>
/// The program.
/// </summary>
public class Program
{
	public static void Main(string[] args)
	{
		Foo(args).GetAwaiter().GetResult();
	}
	
	/// <summary>
	/// Entry point. Initializes the bot.
	/// </summary>
	/// <param name="args">The args.</param>
	private static async Task Foo(string[] args)
	{
		Console.WriteLine("Starting bot...");
		
		var config = JsonConvert.DeserializeObject<ConfigJson>(File.ReadAllText("config.json"));
		if (config.BOT_TOKEN == "" 
		    || config.SPOTIFY_CLIENT_ID == "" || config.SPOTIFY_CLIENT_SECRET == "" 
		    || config.YOUTUBE_API_KEY == "" || config.YOUTUBE_APPLICATION_NAME == "" 
		    || config.LAVALINK_PORT == -1 || config.LAVALINK_PASSWORD == "" || config.LAVALINK_HOSTNAME == "")
		{
			Console.WriteLine("config.json is not configured.");
			return;
		}
		
		YoutubeHandler.youtubeService = new YouTubeService(new BaseClientService.Initializer()
		{
			ApiKey = config.YOUTUBE_API_KEY,
			ApplicationName = config.YOUTUBE_APPLICATION_NAME
		});
		
		var credentials =
			new ClientCredentialsRequest(config.SPOTIFY_CLIENT_ID, config.SPOTIFY_CLIENT_SECRET);
		SpotifyHandler.SpotifyClient = new SpotifyClient(new OAuthClient().RequestToken(credentials).Result);
		
		DiscordConfiguration discordConfiguration = new()
		{
			Token = args[0],
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent
		};

		DiscordClient discordClient = new(discordConfiguration);

		var endpoint = new ConnectionEndpoint
		{
			Hostname = config.LAVALINK_HOSTNAME, // Lavalink server ip.
			Port = config.LAVALINK_PORT // Lavalink server port
		};

		var lavalinkConfig = new LavalinkConfiguration
		{
			Password = config.LAVALINK_PASSWORD, // Lavalink server password.
			RestEndpoint = endpoint,
			SocketEndpoint = endpoint
		};

		var lavalink = discordClient.UseLavalink();
		
		
		var appCommandModule = typeof(ApplicationCommandsModule);
		var commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => appCommandModule.IsAssignableFrom(t) && !t.IsNested).ToList();

		var appCommandExt = discordClient.UseApplicationCommands(new ApplicationCommandsConfiguration
		{
			DebugStartup = true
		});

		// Register event handlers
		appCommandExt.SlashCommandErrored += Slash_SlashCommandErroredAsync;
		
		foreach (var command in commands)
			appCommandExt.RegisterGuildCommands(command, 809493208504860692);
		
		discordClient.Logger.LogInformation("Application commands registered successfully");
		
		Console.WriteLine("Connecting to Discord...");
		await discordClient.ConnectAsync();

		// Use the default logger provided for easy reading
		discordClient.Logger.LogInformation("Connection success! Logged in as {CurrentUserUsername}#{CurrentUserDiscriminator} ({CurrentUserId})", discordClient.CurrentUser.Username, discordClient.CurrentUser.Discriminator, discordClient.CurrentUser.Id);

		// Lavalink
		discordClient.Logger.LogInformation($"Connecting to lavalink...");
		await lavalink.ConnectAsync(lavalinkConfig); // Make sure this is after discordClient.ConnectAsync()
		discordClient.Logger.LogInformation($"Successful connection with lavalink!");
		
		// Listen for commands by putting this method to sleep and relying off of DiscordClient's event listeners
		await Task.Delay(-1);
	}

	/// <summary>
	/// Fires when an exception is thrown in the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandErroredAsync(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
	{
		sender.Client.Logger.LogError("Slash: {ExceptionMessage} | CN: {ContextCommandName} | IID: {ContextInteractionId}", e.Exception.Message, e.Context.CommandName, e.Context.InteractionId);
		return Task.CompletedTask;
	}
}
