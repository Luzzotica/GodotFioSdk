using FioSharp.Core;
using FioSharp.Core.Api.v1;
using FioSharp.Core.Exceptions;
using FioSharp.Core.Helpers;
using FioSharp.Core.Interfaces;
using FioSharp.Core.Providers;
using Godot;
using NUnit.Framework;
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
				Assert.AreEqual(availCheck.is_registered, 1);
				if (availCheck.is_registered != 1) throw new Exception("Failed");

				GD.Print("Testing Succeeded");
			}
			catch (Exception e)
			{
				GD.PrintErr(e.ToString());
			}

			

			//string test = "1. FioSendOBTDataAndBundles";
			//try
			//{
			//	GD.Print(test);
			//	await Setup();
			//	await FioSendOBTDataAndBundles();
			//	GD.Print(test + " Success");
			//}
			//catch (ApiErrorException e)
			//{
			//	GD.Print(test + " Failed: " + e.ToString());
			//}
			//catch (ApiException e)
			//{
			//	GD.Print(test + " Failed" + e.Content);
			//}

			//test = "2. TPIDCryptoHandlesAndDomains";
			//try
			//{
			//	GD.Print(test);
			//	await Setup();
			//	await TPIDCryptoHandlesAndDomains();
			//	GD.Print(test + " Success");
			//}
			//catch (ApiErrorException e)
			//{
			//	GD.Print(test + " Failed: " + e.ToString());
			//}
			//catch (ApiException e)
			//{
			//	GD.Print(test + " Failed: " + e.Content);
			//}

			//test = "3. FioRequest";
			//try
			//{
			//	GD.Print(test);
			//	await Setup();
			//	await FioRequest();
			//	GD.Print(test + " Success");
			//}
			//catch (ApiErrorException e)
			//{
			//	GD.Print(test + " Failed: " + e.ToString());
			//}
			//catch (ApiException e)
			//{
			//	GD.Print(test + " Failed: " + e.Content);
			//}

			//test = "4. OBTData";
			//try
			//{
			//	GD.Print(test);
			//	await Setup();
			//	await OBTData();
			//	GD.Print(test + " Success");
			//}
			//catch (ApiErrorException e)
			//{
			//	GD.Print(test + " Failed: " + e.ToString());
			//}
			//catch (ApiException e)
			//{
			//	GD.Print(test + " Failed: " + e.Content);
			//}

			//test = "5. SignedNFTs";
			//try
			//{
			//	GD.Print(test);
			//	await Setup();
			//	await SignedNFTs();
			//	GD.Print(test + " Success");
			//}
			//catch (ApiErrorException e)
			//{
			//	GD.Print(test + " Failed: " + e.ToString());
			//}
			//catch (ApiException e)
			//{
			//	GD.Print(test + " Failed: " + e.Content);
			//}

			//test = "6. Staking";
			//try
			//{
			//	GD.Print(test);
			//	await Setup();
			//	await Staking();
			//	GD.Print(test + " Success");
			//}
			//catch (ApiErrorException e)
			//{
			//	GD.Print(test + " Failed: " + e.ToString());
			//}
			//catch (ApiException e)
			//{
			//	GD.Print(test + " Failed: " + e.Content);
			//}
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

		/// <summary>
		/// * Send FIO to Public Key with {{trnsfiopubky}}
		/// * Confirm {{get_account}}
		/// * Confirm {{get_fio_balance}}
		/// * Register address with {{regaddress}}
		/// * Map address with {{addaddress}}
		/// * Confirm {{/get_pub_address}}
		/// * Confirm {{/get_pub_addresses}}
		/// * Remove address with {{remaddress}}
		/// * Confirm removal with {{/get_pub_address}}
		/// * Add bundles with {{addbundles}}
		/// * Confirm bundle change with {{/get_fio_names}}  
		/// </summary>
		public async Task FioSendOBTDataAndBundles()
		{
			string fioAddress = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress, fioSdk1);

			// Remove address and confirm it
			try
			{
				await fioSdk1.PushTransaction(new remaddress(
						fioAddress,
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
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}

			try
			{
				GetPubAddressResponse getPubAddress = await fioSdk1.GetFioApi().GetPubAddress(fioAddress, "FIO", "FIO");
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "Public address not found");
			}

			// Add Bundles and confirm they exist
			try
			{
				GetFioNamesResponse resp = await fioSdk1.GetFioNames();
				int bundledTxns = resp.fio_addresses[0].remaining_bundled_tx;
				Assert.Greater(bundledTxns, 0);

				await fioSdk1.PushTransaction(new addbundles(
						fioAddress,
						1,
						defaultFee,
						"",
						fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				resp = await fioSdk1.GetFioNames();
				int newBundledTxns = resp.fio_addresses[0].remaining_bundled_tx;
				Assert.Greater(newBundledTxns, bundledTxns);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}
		}

		/// <summary>
		/// * Generate two key pairs
		/// * Register FIO Crypto Handle to public key #1
		/// * Confirm with {{/get_fio_addresses}}
		///     and {{/avail_check}}
		/// * Register FIO Crypto Handle to public key #2
		/// * Register domain with {{regdomain}} for public key #1
		///     and use the new crypto handle for #2 in the TPID field
		/// * Confirm with {{/get_fio_names}}
		///     and {{/get_fio_domains}}
		///     and {{/avail_check}}
		/// * Confirm balance for #2 account changed because of fee paid to tpid
		/// * Make domain public with {{setdomainpub}}
		/// * Transfer domain with {{xferdomain}}
		/// </summary>
		public async Task TPIDCryptoHandlesAndDomains()
		{
			// Register and map an address, confirm it exists
			string fioAddress = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress, fioSdk1);
			string fioAddress2 = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress2, fioSdk2);

			// Test TPID
			string fioDomain = GenerateTestingFioDomain();
			try
			{
				// Get balance for key 2, compare against later
				GetFioBalanceResponse getFioBalance = await fioSdk2.GetFioBalance();
				ulong currBalance = getFioBalance.balance;

				// Create domain
				await fioSdk1.PushTransaction(new regdomain(
					fioDomain,
					fioSdk1.GetPublicKey(),
					defaultFee,
					fioAddress2,
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Check domain was registered: fio names, avail check, fio domains
				GetFioNamesResponse resp = await fioSdk1.GetFioNames();
				Assert.AreEqual(resp.fio_domains[0].fio_domain, fioDomain);
				GetFioDomainsResponse resp2 = await fioSdk1.GetFioDomains(1, 0);
				Assert.AreEqual(resp2.fio_domains[0].fio_domain, fioDomain);
				AvailCheckResponse availCheck = await fioSdk1.GetFioApi().AvailCheck(fioDomain);
				Assert.AreEqual(availCheck.is_registered, 1);

				// Claim funds from transactions
				await fioSdk2.PushTransaction(new tpidclaim(fioSdk2.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				getFioBalance = await fioSdk2.GetFioBalance();
				ulong newBalance = getFioBalance.balance;
				Assert.Greater(newBalance, currBalance);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}

			// Make domain public and transfer it
			try
			{
				// Register domain
				await fioSdk1.PushTransaction(new setdomainpub(
					fioDomain,
					1, 
					defaultFee,
					fioAddress2,
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Check domain is public
				GetFioDomainsResponse resp2 = await fioSdk1.GetFioDomains(1, 0);
				Assert.AreEqual(resp2.fio_domains[0].fio_domain, fioDomain);
				Assert.AreEqual(resp2.fio_domains[0].is_public, 1);

				// Transfer domain
				await fioSdk1.PushTransaction(new xferdomain(
					fioDomain,
					fioSdk2.GetPublicKey(),
					defaultFee,
					fioAddress2,
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Check domain was transfered
				GetFioNamesResponse resp = await fioSdk1.GetFioNames();
				Assert.AreEqual(resp.fio_domains.Count, 0);
				resp = await fioSdk2.GetFioNames();
				Assert.AreEqual(resp.fio_domains[0].fio_domain, fioDomain);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}
		}

		/// <summary>
		/// * Create two users and register crypto handles for the users
		/// * Request FIO with {{newfundsreq}}
		/// * Confirm {{/get_sent_fio_requests}}
		///     {{/get_received_fio_requests}}
		///     {{/get_pending_fio_requests}}
		///     {{/get_cancelled_fio_requests}}
		/// * Confirm status with {{/get_obt_data}}
		/// * Confirm encrypted content field is properly decrypted by both user keys
		/// * Fulfill the request
		/// * Create {{recordobt}} with the {{fio_request_id}}
		/// * Confirm status with {{/get_obt_data}}
		/// * Confirm {{/ get_sent_fio_requests}}
		///     {{/get_received_fio_requests}}
		///     {{/get_pending_fio_requests}}
		///     {{/get_cancelled_fio_requests}}
		/// * Create new request
		/// * Cancel request
		/// * Confirm {{/get_sent_fio_requests}}
		///     {{/get_received_fio_requests}}
		///     {{/get_pending_fio_requests}}
		///     {{/get_cancelled_fio_requests}}
		/// </summary>
		public async Task FioRequest()
		{
			// Register and map an address, confirm it exists
			string fioAddress = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress, fioSdk1);
			string fioAddress2 = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress2, fioSdk2);

			Dictionary<string, object> newFundsContent = new newfundsreq_content(
				fioSdk1.GetPublicKey(),
				"1",
				"FIO",
				"FIO",
				memo: "Swag").ToJsonObject();

			FioRequest req = null;

			// Request fio
			try
			{
				
				string encryptedContent = await fioSdk1.DHEncrypt(
					fioSdk2.GetPublicKey(),
					FioHelper.NEW_FUNDS_CONTENT,
					newFundsContent);

				await fioSdk1.PushTransaction(new newfundsreq(
					fioAddress2,
					fioAddress,
					"",
					encryptedContent,
					defaultFee,
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Confirm request was sent, received, and nothing was cancelled
				GetSentFioRequestsResponse getSentFioRequests = await fioSdk1.GetSentFioRequests(1, 0);
				Assert.Greater(getSentFioRequests.requests.Count, 0);
				GetReceivedFioRequestsResponse getReceivedFioRequests = await fioSdk2.GetReceivedFioRequests(1, 0);
				Assert.Greater(getReceivedFioRequests.requests.Count, 0);
				GetPendingFioRequestsResponse getPendingFioRequests = await fioSdk2.GetPendingFioRequests(1, 0);
				Assert.Greater(getPendingFioRequests.requests.Count, 0);

				req = getReceivedFioRequests.requests[0];
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}

			try
			{
				GetPendingFioRequestsResponse getPendingFioRequests = await fioSdk1.GetPendingFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}

			try
			{
				GetCancelledFioRequestsResponse getCancelledFioRequests = await fioSdk1.GetCancelledFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}

			// Fulfill request, record OBT data
			try
			{
				// Confirm encrypted content field is properly decrypted by both user keys
				Dictionary<string, object> dContent = await fioSdk1.DHDecrypt(
					fioSdk2.GetPublicKey(), FioHelper.NEW_FUNDS_CONTENT, req.content);
				Dictionary<string, object> dContent2 = await fioSdk2.DHDecrypt(
					fioSdk1.GetPublicKey(), FioHelper.NEW_FUNDS_CONTENT, req.content);
				Assert.AreEqual(newFundsContent["memo"], dContent["memo"]);
				Assert.AreEqual(newFundsContent["memo"], dContent2["memo"]);
				Assert.AreEqual(dContent, dContent2);

				// 1. Fulfill the request (Transfer funds)
				// 2. Record OBT data with fio request id
				PushTransactionResponse txn = await fioSdk2.PushTransaction(new trnsfiopubky(
					dContent2["payee_public_address"].ToString(),
					ulong.Parse(dContent2["amount"].ToString()),
					defaultFee,
					"",
					fioSdk2.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait); 

				Dictionary<string, object> recordObtContent = new recordobt_content(
					fioSdk2.GetPublicKey(),
					dContent2["payee_public_address"].ToString(),
					dContent2["amount"].ToString(),
					dContent2["chain_code"].ToString(),
					dContent2["token_code"].ToString(),
					"sent_to_blockchain",
					txn.transaction_id,
					"Transferred").ToJsonObject();
				string encryptedContent2 = await fioSdk1.DHEncrypt(
					fioSdk2.GetPublicKey(),
					FioHelper.RECORD_OBT_DATA_CONTENT,
					recordObtContent);
				await fioSdk2.PushTransaction(new recordobt(
					req.payer_fio_address,
					req.payee_fio_address,
					encryptedContent2,
					req.fio_request_id.ToString(),
					defaultFee,
					"",
					fioSdk2.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Confirm obt data
				GetObtDataResponse getObtData = await fioSdk2.GetObtData(1, 0);
				ObtData obtData = getObtData.obt_data_records[0];
				Assert.AreEqual(obtData.fio_request_id, req.fio_request_id);
				Assert.AreEqual(obtData.status, "sent_to_blockchain");
				// Confirm encrypted content field is properly decrypted by both user keys
				Dictionary<string, object> obtDContent = await fioSdk1.DHDecrypt(
					fioSdk2.GetPublicKey(), FioHelper.RECORD_OBT_DATA_CONTENT, obtData.content);
				Dictionary<string, object> obtDContent2 = await fioSdk2.DHDecrypt(
					fioSdk1.GetPublicKey(), FioHelper.RECORD_OBT_DATA_CONTENT, obtData.content);
				Assert.AreEqual(recordObtContent["memo"], obtDContent["memo"]);
				Assert.AreEqual(recordObtContent["memo"], obtDContent2["memo"]);
				Assert.AreEqual(obtDContent, obtDContent2);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}

			try
			{
				GetSentFioRequestsResponse getSentFioRequests = await fioSdk1.GetSentFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}
			try
			{
				GetReceivedFioRequestsResponse getReceivedFioRequests = await fioSdk2.GetReceivedFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}
			try
			{
				GetPendingFioRequestsResponse getPendingFioRequests = await fioSdk2.GetPendingFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}
			try
			{
				GetCancelledFioRequestsResponse getCancelledFioRequests = await fioSdk1.GetCancelledFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}

			// Request and cancel
			try
			{
				string encryptedContent = await fioSdk1.DHEncrypt(
					fioSdk2.GetPublicKey(),
					FioHelper.NEW_FUNDS_CONTENT,
					newFundsContent);

				await fioSdk1.PushTransaction(new newfundsreq(
					fioAddress2,
					fioAddress,
					"",
					encryptedContent,
					defaultFee,
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Confirm request was sent, received, and nothing was cancelled
				GetSentFioRequestsResponse getSentFioRequests = await fioSdk1.GetSentFioRequests(2, 1);
				Assert.Greater(getSentFioRequests.requests.Count, 0);
				req = getSentFioRequests.requests[0];
				GetPendingFioRequestsResponse getPendingFioRequests = await fioSdk2.GetPendingFioRequests(1, 0);
				Assert.Greater(getPendingFioRequests.requests.Count, 0);

				await fioSdk1.PushTransaction(new cancelfndreq(
					req.fio_request_id.ToString(),
					defaultFee,
					"",
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				GetCancelledFioRequestsResponse getCancelledFioRequests = await fioSdk1.GetCancelledFioRequests(1, 0);
				Assert.Greater(getCancelledFioRequests.requests.Count, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}

			try
			{
				GetSentFioRequestsResponse get = await fioSdk1.GetSentFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}
			try
			{
				GetPendingFioRequestsResponse getPendingFioRequests = await fioSdk2.GetPendingFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}
			try
			{
				GetReceivedFioRequestsResponse getReceivedFioRequests = await fioSdk2.GetReceivedFioRequests(1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No FIO Requests");
			}
		}

		/// <summary>
		/// * Create two users and register crypto handles for the users
		/// * Send FIO from user1 to user2 with {{trnsfiopubky}}
		/// * Add transaction metadata with {{recordobt}}
		/// * Confirm status with {{/get_obt_data}}
		/// * Confirm encrypted content field is properly decrypted by both user keys  
		/// </summary>
		public async Task OBTData()
		{
			// Register and map an address, confirm it exists
			string fioAddress = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress, fioSdk1);
			string fioAddress2 = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress2, fioSdk2);

			// Send fio, record OBT, decrypt
			try
			{
				// Get public key from mapped address
				GetPubAddressResponse getFioAddresses = await fioSdk2.GetFioApi().GetPubAddress(fioAddress, "FIO", "FIO");
				
				PushTransactionResponse txn = await fioSdk2.PushTransaction(new trnsfiopubky(
					getFioAddresses.public_address,
					FioSdk.AmountToSUF(10),
					defaultFee,
					"",
					fioSdk2.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				Dictionary<string, object> recordObtContent = new recordobt_content(
					getFioAddresses.public_address,
					fioSdk1.GetPublicKey(),
					FioSdk.AmountToSUF(10).ToString(),
					"FIO",
					"FIO",
					"sent_to_blockchain",
					txn.transaction_id,
					"Transferred").ToJsonObject();
				string encryptedContent2 = await fioSdk2.DHEncrypt(
					getFioAddresses.public_address,
					FioHelper.RECORD_OBT_DATA_CONTENT,
					recordObtContent);
				await fioSdk2.PushTransaction(new recordobt(
					fioAddress2,
					fioAddress,
					encryptedContent2,
					"",
					defaultFee,
					"",
					fioSdk2.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Confirm obt data
				GetObtDataResponse getObtData = await fioSdk2.GetObtData(1, 0);
				ObtData obtData = getObtData.obt_data_records[0];
				Assert.AreEqual(obtData.status, "sent_to_blockchain");
				// Confirm encrypted content field is properly decrypted by both user keys
				Dictionary<string, object> obtDContent = await fioSdk1.DHDecrypt(
					fioSdk2.GetPublicKey(), FioHelper.RECORD_OBT_DATA_CONTENT, obtData.content);
				Dictionary<string, object> obtDContent2 = await fioSdk2.DHDecrypt(
					fioSdk1.GetPublicKey(), FioHelper.RECORD_OBT_DATA_CONTENT, obtData.content);
				Assert.AreEqual(recordObtContent["memo"], obtDContent["memo"]);
				Assert.AreEqual(recordObtContent["memo"], obtDContent2["memo"]);
				Assert.AreEqual(obtDContent, obtDContent2);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}
		}

		/// <summary>
		/// * Create fio handle
		/// * Create signature with {{addnft}}
		/// * Confirm with {{/get_nfts_fio_address}}
		///     {{/get_nfts_contract}}
		///     {{/get_nfts_hash}}
		/// * Remove signature with {{remnft}}
		/// * Confirm with {{/get_nfts_fio_address}}
		///     {{/get_nfts_contract}}
		///     {{/get_nfts_hash}}
		/// </summary>
		public async Task SignedNFTs()
		{
			// Register and map an address, confirm it exists
			string fioAddress = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress, fioSdk1);
			string hash = GenerateHashForNft();
			string chainCode = "A",
				contractAddress = "B",
				tokenId = "1",
				url = "https://service.invalid.com";

			try
			{
				await fioSdk1.PushTransaction(new addnft(fioAddress,
					new List<object> { new NFTData {
						chain_code = chainCode,
						contract_address = contractAddress,
						token_id = tokenId,
						url = url,
						hash = hash,
						metadata = ""
					} },
					defaultFee,
					fioSdk1.GetActor(),
					""));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				GetNftsFioAddressResponse getNftsFioAddress = await fioSdk1.GetFioApi().GetNftsFioAddress(fioAddress, 1, 0);
				Assert.AreEqual(getNftsFioAddress.nfts[0].chain_code, chainCode);
				Assert.AreEqual(getNftsFioAddress.nfts[0].contract_address, contractAddress);
				Assert.AreEqual(getNftsFioAddress.nfts[0].token_id, tokenId);
				Assert.AreEqual(getNftsFioAddress.nfts[0].hash, hash);
				GetNftsContractResponse getNftsContract = await fioSdk1.GetFioApi().GetNftsContract(chainCode, contractAddress, tokenId, 1, 0);
				Assert.AreEqual(getNftsContract.nfts[0].chain_code, chainCode);
				Assert.AreEqual(getNftsContract.nfts[0].contract_address, contractAddress);
				Assert.AreEqual(getNftsContract.nfts[0].token_id, tokenId);
				Assert.AreEqual(getNftsContract.nfts[0].hash, hash);
				GetNftsHashResponse getNftsHash = await fioSdk1.GetFioApi().GetNftsHash(hash, 1, 0);
				Assert.AreEqual(getNftsHash.nfts[0].chain_code, chainCode);
				Assert.AreEqual(getNftsHash.nfts[0].contract_address, contractAddress);
				Assert.AreEqual(getNftsHash.nfts[0].token_id, tokenId);
				Assert.AreEqual(getNftsHash.nfts[0].hash, hash);

				// Delete the NFT
				await fioSdk1.PushTransaction(new remnft(fioAddress,
					new List<object> { new NFTData {
						chain_code = chainCode,
						contract_address = contractAddress,
						token_id = tokenId,
						url = url,
						hash = hash,
						metadata = ""
					} },
					defaultFee,
					fioSdk1.GetActor(),
					""));

				// Wait for block to confirm
				await Task.Delay(defaultWait);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}

			try
			{
				GetNftsFioAddressResponse getNftsFioAddress = await fioSdk1.GetFioApi().GetNftsFioAddress(fioAddress, 1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No NFTS are mapped");
			}
			try
			{
				GetNftsContractResponse getNftsContract = await fioSdk1.GetFioApi().GetNftsContract(chainCode, contractAddress, tokenId, 1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No NFTS are mapped");
			}
			try
			{
				GetNftsHashResponse getNftsHash = await fioSdk1.GetFioApi().GetNftsHash(hash, 1, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.AreEqual(e.message, "No NFTS are mapped");
			}
		}

		/// <summary>
		/// * Register fio handle
		/// * Vote on a BP
		/// * Stake FIO tokens with {{stakefio}}
		/// * Confirm with {{get_fio_balance}}
		/// * Unstake portion of staked FIO with {{unstakefio}}
		/// * Confirm with {{get_fio_balance}}
		/// </summary>
		public async Task Staking()
		{
			string fioAddress = GenerateTestingFioAddress();
			await RegisterFioHandleToSdk(fioAddress, fioSdk1);
			Console.WriteLine(fioAddress);

			try
			{
				await fioSdk1.PushTransaction(new voteproducer(
					new List<object> { "bp1@dapixdev" },
					fioAddress,
					fioSdk1.GetActor(),
					defaultFee));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Get the current staked, it should be 0
				GetFioBalanceResponse getFioBalance = await fioSdk1.GetFioBalance();
				Assert.AreEqual(0, getFioBalance.staked);

				await fioSdk1.PushTransaction(new stakefio(FioSdk.AmountToSUF(100),
					fioAddress,
					defaultFee,
					"",
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Get the staked amount now, it should be 100
				getFioBalance = await fioSdk1.GetFioBalance();
				Assert.AreEqual(FioSdk.AmountToSUF(100), getFioBalance.staked);

				await fioSdk1.PushTransaction(new unstakefio(FioSdk.AmountToSUF(100),
					fioAddress,
					defaultFee,
					"",
					fioSdk1.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Get the staked amount, it should be less than 100 (Technically 0)
				getFioBalance = await fioSdk1.GetFioBalance();
				Assert.AreEqual(getFioBalance.staked, 0);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}
		}

		public async Task RegisterFioHandleToSdk(string handle, FioSdk sdk, bool fund = true)
		{
			try
			{
				if (fund)
				{
					// Fund the new account
					await fioFaucet.PushTransaction(new trnsfiopubky(
						sdk.GetPublicKey(),
						defaultFundAmount,
						defaultFee,
						"",
						fioFaucet.GetActor()));

					GetFioBalanceResponse getFioBalance = await sdk.GetFioBalance();
					Assert.AreEqual(getFioBalance.balance, defaultFundAmount);
				}

				// Register and map
				await sdk.PushTransaction(new regaddress(
					handle,
					sdk.GetPublicKey(),
					defaultFee,
					"",
					sdk.GetActor()));
				await Task.Delay(defaultWait);
				await sdk.PushTransaction(new addaddress(
					handle,
					new List<object> { new Dictionary<string, object>{
						{ "chain_code", "FIO" },
						{ "token_code", "FIO" },
						{ "public_address", sdk.GetPublicKey() }
					} },
					defaultFee,
					"",
					sdk.GetActor()));

				// Wait for block to confirm
				await Task.Delay(defaultWait);

				// Check address exists, and that the key is correct
				GetFioAddressesResponse getFioAddresses = await sdk.GetFioAddresses(1, 0);
				Assert.Greater(getFioAddresses.fio_addresses.Count, 0);
				Assert.AreEqual(getFioAddresses.fio_addresses[0].fio_address, handle);
				GetPubAddressResponse getPubAddress = await sdk.GetFioApi().GetPubAddress(handle, "FIO", "FIO");
				Assert.AreEqual(getPubAddress.public_address, sdk.GetPublicKey());
				GetPubAddressesResponse getPubAddresses = await sdk.GetFioApi().GetPubAddresses(handle, 1, 0);
				Assert.AreEqual(getPubAddresses.public_addresses[0].public_address, sdk.GetPublicKey());
				AvailCheckResponse availCheck = await sdk.GetFioApi().AvailCheck(handle);
				Assert.AreEqual(availCheck.is_registered, 1);
			}
			catch (ApiErrorException e)
			{
				Assert.Fail(e.ToString() + "\n" + e.StackTrace);
			}
		}
	}
}


