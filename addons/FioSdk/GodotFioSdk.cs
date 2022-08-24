using FioSharp;
using Godot;
using System;
using System.Threading.Tasks;

public class GodotFioSdk : Node
{
	public FioSdk fioSdk;
	private GodotHttpHandler handler;

	[Signal]
	delegate void SetupComplete(bool success, string message);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		handler = GetNode<GodotHttpHandler>("GodotHttpHandler");
	}

	public async Task Setup(string url, string privateKey)
	{
		try
		{
			fioSdk = new FioSdk(
				privateKey,
				url,
				httpHandler: handler
			);
			await fioSdk.Init();
		}
		catch (Exception e)
		{
			EmitSignal("SetupComplete", false, e.ToString());
		}

		EmitSignal("SetupComplete", true, "");
	}
}
