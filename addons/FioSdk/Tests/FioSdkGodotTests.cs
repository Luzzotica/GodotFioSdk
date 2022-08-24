using FioSharp.Core;
using FioSharp.Core.Api.v1;
using FioSharp.Core.Exceptions;
using FioSharp.Core.Helpers;
using FioSharp.Core.Interfaces;
using FioSharp.Core.Providers;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FioSharp.Godot
{
	public class FioSdkGodotTests : Node
	{
		FioSdk fioFaucet;
		FioSdk fioSdk1;
		FioSdk fioSdk2;

		const string devnetUrl = "http://52.40.41.71:8889";
		const string faucetPrivKey = "5KF2B21xT5pE5G3LNA6LKJc6AP2pAd2EnfpAUrJH12SFV8NtvCD";

		const int defaultWait = 1000;

		readonly ulong defaultFee = FioSdk.AmountToSUF(1000);
		readonly ulong defaultFundAmount = FioSdk.AmountToSUF(1000);

		private string GetDateTimeNowMillis()
		{
			return ((long)DateTime.Now.ToUniversalTime().Subtract(
				new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
				).TotalMilliseconds).ToString();
		}

		private string GenerateTestingFioDomain()
		{
			return "testing-domain-" + GetDateTimeNowMillis();
		}

		private string GenerateTestingFioAddress(string domain = "fiotestnet")
		{
			return "testing" + GetDateTimeNowMillis() + "@" + domain;
		}

		private string GenerateObtId()
		{
			return GetDateTimeNowMillis();
		}

		private string GenerateHashForNft()
		{
			string now = GetDateTimeNowMillis().Substring(0, 13);
			return "f83b5702557b1ee76d966c6bf92ae0d038cd176aaf36f86a18e" + now;
		}

		/// <summary>
		/// * Generate a key pair for sender and receiver
		/// </summary>
		public override void _Ready()
		{
			// Get the godot handler
			RunTests();
		}

		public async Task RunTests()
		{
			// Test the godot http handler
			await Setup();

			string handle = GenerateTestingFioAddress();

			try
			{
				GD.Print("Testing GodotHttpHandler");
				// Fund the new account
				await fioFaucet.PushTransaction(new trnsfiopubky(
					fioSdk1.GetPublicKey(),
					defaultFundAmount,
					defaultFee,
					"",
					fioFaucet.GetActor()));

				GetFioBalanceResponse getFioBalance = await fioSdk1.GetFioBalance();
				if (getFioBalance.balance != defaultFundAmount) throw new Exception("Failed");

				// Register and map
				await fioSdk1.PushTransaction(new regaddress(
					handle,
					fioSdk1.GetPublicKey(),
					defaultFee,
					"",
					fioSdk1.GetActor()));
				await Task.Delay(defaultWait);
				await fioSdk1.PushTransaction(new addaddress(
					handle,
					new List<object> { new Dictionary<string, object>{
						{ "chain_code", "FIO" },
						{ "token_code", "FIO" },
						{ "public_address", fioSdk1.GetPublicKey() }
					} },
					defaultFee,
					"",
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Check address exists, and that the key is correct
				GetFioAddressesResponse getFioAddresses = await fioSdk1.GetFioAddresses(1, 0);
				if (getFioAddresses.fio_addresses.Count == 0) throw new Exception("Failed");
				if (!getFioAddresses.fio_addresses[0].fio_address.Equals(handle)) throw new Exception("Failed");
				GetPubAddressResponse getPubAddress = await fioSdk1.GetFioApi().GetPubAddress(handle, "FIO", "FIO");
				if (!getPubAddress.public_address.Equals(fioSdk1.GetPublicKey())) throw new Exception("Failed");
				GetPubAddressesResponse getPubAddresses = await fioSdk1.GetFioApi().GetPubAddresses(handle, 1, 0);
				if (!getPubAddresses.public_addresses[0].public_address.Equals(fioSdk1.GetPublicKey())) throw new Exception("Failed");
				AvailCheckResponse availCheck = await fioSdk1.GetFioApi().AvailCheck(handle);
				if (availCheck.is_registered != 1) throw new Exception("Failed");

				GD.Print("Testing Succeeded");
			}
			catch (Exception e)
			{
				GD.PrintErr(e.ToString());
			}
		}
		
		public async Task Setup()
		{
			GodotHttpHandler handler = GetNode<GodotHttpHandler>("HttpHandler");
			fioFaucet = new FioSdk(
				faucetPrivKey,
				devnetUrl,
				httpHandler: handler
			);
			await fioFaucet.Init();

			// Generate a new key for both of these bad boys
			string privKey1 = FioSdk.CreatePrivateKey().fioPrivateKey;
			string privKey2 = FioSdk.CreatePrivateKey().fioPrivateKey;
			//Console.WriteLine("Priv Key 1: " + privKey1);
			//Console.WriteLine("Priv Key 2: " + privKey2);
			
			fioSdk1 = new FioSdk(
				privKey1,
				devnetUrl,
				httpHandler: handler
			);
			fioSdk2 = new FioSdk(
				privKey2,
				devnetUrl,
				httpHandler: handler
			);
			await fioSdk1.Init();
			await fioSdk2.Init();
			//Console.WriteLine("Pub Key 1: " + fioSdk1.GetPublicKey());
			//Console.WriteLine("Pub Key 2: " + fioSdk1.GetPublicKey());
		}
	}
}


