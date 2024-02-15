﻿using MauiTickets.Databases;
using Microsoft.Maui.Animations;
using System.Net.Http.Json;
using TicketLibrary.Data;
using TicketLibrary.Services;

namespace MauiTickets.Services;

public class MauiTicketService : ITicketService
{
    HttpClient client = new HttpClient() { BaseAddress = new Uri("https://blazortickets20240214165128.azurewebsites.net")};
    public ticketAppDb ticketAppDb { get; set; }

    public MauiTicketService(ticketAppDb db)
    {
        ticketAppDb = db;
        SyncDatabases();
        SetTimer(20);
    }


    public async Task<List<Ticket>> GetAllTicketsAsync()
    {
        await Task.CompletedTask;
        return ticketAppDb.Connection.Table<Ticket>().ToList();
    }

    public Task AddATicket(Ticket ticket)
    {
        ticketAppDb.Connection.Insert(ticket);
        return Task.CompletedTask;
    }

    public Task UpdateATicket(Ticket ticket)
    {
        ticketAppDb.Connection.Update(ticket);
        return Task.CompletedTask;  
    }


    public void SetTimer(int seconds)
    {
        var startTimeSpan = TimeSpan.Zero;
        var periodTimeSpan = TimeSpan.FromSeconds(seconds);

        var timer = new System.Threading.Timer((e) =>
        {
            SyncDatabases();
        }, null, startTimeSpan, periodTimeSpan);
    }


    public async Task SyncDatabases()
    {
        List<Ticket> onlineTickets = await client.GetFromJsonAsync<List<Ticket>>("/api/Ticket/getall");
        List<Ticket> localTickets = await GetAllTicketsAsync();

        List<Ticket> temp = new List<Ticket>();

        SyncOnlineToLocal(onlineTickets, localTickets);
        SyncLocalToOnline(onlineTickets, localTickets);

    }

    private void SyncOnlineToLocal(List<Ticket> onlineTickets, List<Ticket> localTickets)
    {
        Ticket tempTicket = new();

        foreach (Ticket ticket in onlineTickets)
        {
            if (localTickets.FirstOrDefault(q => q.Id == ticket.Id) is null)
            {
                tempTicket = ticket;
                tempTicket.Id = ticket.Id;
                AddATicket(tempTicket);
            }
            else
            {
                if ((localTickets.FirstOrDefault(q => q.Id == ticket.Id).IsScanned) != ticket.IsScanned)
                {
                    //set the local ticket equal to online
                    UpdateATicket(ticket);
                }
            }
        }
    }

    public async Task SyncLocalToOnline(List<Ticket> onlineTickets, List<Ticket> localTickets)
    {
        foreach (Ticket ticket in localTickets)
        {
            if (onlineTickets.FirstOrDefault(q => q.Id == ticket.Id) is null)
            {
                await client.PostAsJsonAsync("/api/Ticket/addticket", ticket);
            }
            else
            {
                if ((onlineTickets.FirstOrDefault(q => q.Id == ticket.Id).IsScanned) != ticket.IsScanned)
                {
                    //set the online ticket equal to local
                    await client.PutAsJsonAsync("/api/Ticket/updateticket", ticket);
                }
            }
        }
    }
}
