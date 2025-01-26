using System;
using System.Threading;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;

using System.Collections.Generic;
using System.Text;
using System.Net.Security;
using System.Reflection.Metadata;

class Program
{



    //main program functions
    static async Task Main(string[] args)
    {
        /*
        var data = await Get_messages("", "http://127.0.0.1:8000/data");
        Console.WriteLine(data);
        foreach (var row in data)
        {
            Console.WriteLine(string.Join(", ", row));
        }
        */
        Dictionary<string, string> app_info = info_setup();
       
        BranchesController(app_info).GetAwaiter().GetResult();
    }

    static async Task BranchesController(Dictionary<string, string> appinfo)
    {
        dynamic text = new System.Dynamic.ExpandoObject();
        dynamic messages = new System.Dynamic.ExpandoObject();
        dynamic changed = new System.Dynamic.ExpandoObject();
        dynamic app_info = new System.Dynamic.ExpandoObject();

        app_info.value = appinfo;
        changed.value = true;
        //messages.values = fill_messages();  // Shared variable
        messages.values = await Get_messages(app_info.value["Token"], app_info.value["IP"] + "/getchat");
        text.values = new List<string>();
        text.value = "";


        var updateTask = update_messages(messages, changed, app_info);
        var mainTask = MainLoop(messages, text, changed, app_info);
        var userInput = userinput(text, changed, app_info);

        await Task.WhenAny(updateTask, mainTask, userInput);
    }

    // app setup

    static Dictionary<string, string> info_setup()
    {

        Dictionary<string, string> info = new Dictionary<string, string>();

        Console.WriteLine("Enter Server IP:");
        info.Add("IP", Console.ReadLine());
        Console.WriteLine("Enter Server Key:");
        info.Add("Key", Console.ReadLine());
        Console.WriteLine("Enter User Name:");
        info.Add("User", Console.ReadLine());
        Console.WriteLine("Enter User Password:");
        info.Add("Password", Console.ReadLine());

        if (true)
        {
            info["IP"] = "http://127.0.0.1:8000";
            info["User"] = "tim";
            info["Password"] = "tim1234";
        }
        info["IP"] = "http://127.0.0.1:8000";

        string token = GetToken(info).GetAwaiter().GetResult();
        info.Add("Token", token);
        Console.WriteLine(info["Token"]);

        return info;
    }

    //set_up functions
    static List<List<string>> fill_messages()
    {
        List<List<string>> messages = new List<List<string>>();
        List<string> message = new List<string>();

        message.Add("");
        message.Add("");
        message.Add("");

        for (int i = 0; i < 6; i++)
        {
            messages.Add(message);
        }
        return messages;
    }
    //chat functions

    static async Task update_messages(dynamic messages, dynamic changed, dynamic app_info)
    {
        while (true)
        {
            messages.values = await Get_messages(app_info.value["Token"], app_info.value["IP"] + "/getchat");  // Update the variable
            await Task.Delay(1000);
            //Console.WriteLine("update " + var.value);
            changed.value = true;
        }
    }

    static async Task userinput(dynamic text, dynamic changed, dynamic app_info)
    {
        text.value = "";
        string sequence = "";
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey();

            if (key.Key == ConsoleKey.Enter)
            {
                if (sequence != "")
                {
                    sendMessage(app_info.value["Token"], app_info.value["IP"] + "/newmsg", sequence);

                    text.value = "";
                    sequence = "";
                    changed.value = true;
                }
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                sequence = sequence.Remove(sequence.Length - 1);
                text.value = sequence;
                changed.value = true;
            }
            else if (key.Key == ConsoleKey.Q && (key.Modifiers & ConsoleModifiers.Control) != 0)
            {
                break;
            }
            else if (sequence.Length < 107)
            {
                sequence = sequence + key.KeyChar;
                text.value = sequence;
                changed.value = true;
            }else
            {
               
            }

            //Console.WriteLine(sequence);
            await Task.Delay(0);

        }
    }

    static async Task MainLoop(dynamic messages, dynamic text, dynamic changed, dynamic app_info)
    {

        text.value = "start";
        while (true)
        {
            if (changed.value)
            {

                Console.Clear();

                Console.WriteLine("┌──────────┬────────────────────────────────────┬──────────┐");
                Console.WriteLine("│  Sender  │              messages              │   You    │");
                Console.WriteLine("├──────────┼────────────────────────────────────┼──────────┤");
                messageRenderer(messages.values[5][1], messages.values[5][0], messages.values[5][2]);
                Console.WriteLine("├──────────┼────────────────────────────────────┼──────────┤");
                messageRenderer(messages.values[4][1], messages.values[4][0], messages.values[4][2]);
                Console.WriteLine("├──────────┼────────────────────────────────────┼──────────┤");
                messageRenderer(messages.values[3][1], messages.values[3][0], messages.values[3][2]);
                Console.WriteLine("├──────────┼────────────────────────────────────┼──────────┤");
                messageRenderer(messages.values[2][1], messages.values[2][0], messages.values[2][2]);
                Console.WriteLine("├──────────┼────────────────────────────────────┼──────────┤");
                messageRenderer(messages.values[1][1], messages.values[1][0], messages.values[1][2]);
                Console.WriteLine("├──────────┼────────────────────────────────────┼──────────┤");
                messageRenderer(messages.values[0][1], messages.values[0][0], messages.values[0][2]);
                Console.WriteLine("└──────────┴────────────────────────────────────┴──────────┘\n");



                Console.WriteLine("┌──────────┬────────────────────────────────────┬──────────┐");
                messageRenderer(text.value, me: "Send");
                Console.WriteLine("└──────────┴────────────────────────────────────┴──────────┘");

                await Task.Delay(10);
                changed.value = false;
            }
        }
    }

    static void messageRenderer(string message = "", string user = "", string me = "")
    {
        string empty = "";

        if (message.Length < 36)
        {
            Console.Write("│{0, -10}│", user);
            Console.Write("{0, -36}", message);
            Console.Write("│{0, -10}│\n", me);
        }
        else if (message.Length < 72)
        {
            Console.Write("│{0, -10}│", user);
            Console.Write("{0, -36}", message.Substring(0, 36));
            Console.Write("│{0, -10}│\n", me);
            Console.Write("│{0, -10}│", "");
            Console.Write("{0, -36}", message.Substring(36, message.Length - 36));
            Console.Write("│{0, -10}│\n", "");
        }
        else if (message.Length < 108)
        {
            Console.Write("│{0, -10}│", user);
            Console.Write("{0, -36}", message.Substring(0, 36));
            Console.Write("│{0, -10}│\n", "");
            Console.Write("│{0, -10}│", "");
            Console.Write("{0, -36}", message.Substring(36, 36));
            Console.Write("│{0, -10}│\n", me);
            Console.Write("│{0, -10}│", "");
            Console.Write("{0, -36}", message.Substring(72, message.Length - 72));
            Console.Write("│{0, -10}│\n", "");
        }
        else
        {

            throw new InvalidOperationException("Message too long");

        }

        return;
    }


    // HTTP managment

        //authentication
    public static async Task<string> GetToken(Dictionary<string, string> app_info)
    {
        using (var client = new HttpClient())
        {

            var content = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("username", app_info["User"]),
            new KeyValuePair<string, string>("password", app_info["Password"])
            });

            var response = await client.PostAsync(app_info["IP"] + "/token", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Login failed: " + response.ReasonPhrase);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            return tokenResponse.GetProperty("access_token").GetString();
        }
    }

        //messages loading
    static async Task<List<List<string>>> Get_messages(string token, string adress)
    {

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(adress);
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<List<string>>>(jsonData);
            }
            else
            {
                throw new Exception($"Failed to retrieve data: {response.StatusCode}");
            }
        }
    }

        //message send

    static async Task sendMessage(string token, string adress, string message) { 
    
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new StringContent($"{{\"msg\":\"{message}\"}}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(adress, content);

        if (response.IsSuccessStatusCode)
        {
            return;
        }
        else
        {
            throw new Exception($"Failed to retrieve data: {response.StatusCode}");
        }
    }
}
    
