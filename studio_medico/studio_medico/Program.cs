using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Threading;



namespace Server_StudioMedico
{
    internal class ServerMedico
    {
        static int port = 12345;
        static string dbPath = "Data Source=clinica.db;";

        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine("Server dello studio medico avviato su porta " + port);

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Client connesso: " + ((IPEndPoint)client.Client.RemoteEndPoint).Address);
                Thread t = new Thread(HandleClient);
                t.Start(client);
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[2048];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    string[] parts = msg.Split('|');
                    if (parts.Length < 2)
                    {
                        SendResponse(stream, "Input non valido");
                        continue;
                    }

                    if (!int.TryParse(parts[0], out int service))
                    {
                        SendResponse(stream, "Servizio non valido");
                        continue;
                    }
                    

                    string response = service switch
                    {
                        0 => Autenticazione(parts[1], parts[2]),
                        1 => GetAppuntamenti(parts[1]),
                        2 => GetStoriaClinica(parts[1]),
                        3 => RegistraVisita(parts),
                        4 => InserisciCertificato(parts),
                        _ => "Servizio non valido"
                    };

                    SendResponse(stream, response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore: " + ex.Message);
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }

        static void SendResponse(NetworkStream stream, string response)
        {
            byte[] responseBuffer = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBuffer, 0, responseBuffer.Length);
        }
        static string Autenticazione(string medicoId, string password)
        {
            try
            {
                string dbPath = "Data Source=clinica.db"; 

                using (SqliteConnection conn = new SqliteConnection(dbPath))
                {
                    conn.Open();
                    string query = "SELECT PasswordHash FROM Medico WHERE Matricola = @medicoId";
                    using (SqliteCommand cmd = new SqliteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@medicoId", medicoId);
                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            string storedHash = result.ToString();

                            
                            if (storedHash == password) 
                                return "OK";
                            
                        }
                    }
                }
                return "Credenziali non valide";
            }
            catch (Exception ex)
            {
                return "Errore durante l'autenticazione: " + ex.Message;
            }
}
        static string GetAppuntamenti(string medicoId)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(dbPath))
                {
                    conn.Open();
                    string query = "SELECT Data, Ora, Paziente FROM Appuntamento WHERE Medico = @medicoId";
                    SqliteCommand cmd = new SqliteCommand(query, conn);
                    cmd.Parameters.AddWithValue("@medicoId", medicoId);
                    SqliteDataReader reader = cmd.ExecuteReader();
                    StringBuilder result = new StringBuilder();
                    while (reader.Read())
                    {
                        result.AppendLine($"{reader["Data"]} {reader["Ora"]} Paziente: {reader["Paziente"]}");
                    }
                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                return "Errore durante il recupero degli appuntamenti: " + ex.Message;
            }
        }

        static string GetStoriaClinica(string pazienteId)
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(dbPath))
                {
                    conn.Open();
                    string query = "SELECT Data, Diagnosi, Prescrizioni FROM Visita WHERE Paziente = @pazienteId";
                    SqliteCommand cmd = new SqliteCommand(query, conn);
                    cmd.Parameters.AddWithValue("@pazienteId", pazienteId);
                    SqliteDataReader reader = cmd.ExecuteReader();
                    StringBuilder result = new StringBuilder();
                    while (reader.Read())
                    {
                        result.AppendLine($"{reader["Data"]}: {reader["Diagnosi"]}, Prescrizioni: {reader["Prescrizioni"]}");
                    }
                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                return "Errore durante il recupero della storia clinica: " + ex.Message;
            }
        }

        static string RegistraVisita(string[] data)
        {
            if (data.Length < 8)
            {
                return "Dati insufficienti per registrare la visita";
            }

            try
            {
                using (SqliteConnection conn = new SqliteConnection(dbPath))
                {
                    conn.Open();
                    string query = "INSERT INTO Visita (Data, Ora, Motivo, Diagnosi, Prescrizioni, Medico, Paziente) VALUES (@data, @ora, @motivo, @diagnosi, @prescrizioni, @medico, @paziente)";
                    SqliteCommand cmd = new SqliteCommand(query, conn);
                    cmd.Parameters.AddWithValue("@data", data[1]);
                    cmd.Parameters.AddWithValue("@ora", data[2]);
                    cmd.Parameters.AddWithValue("@motivo", data[3]);
                    cmd.Parameters.AddWithValue("@diagnosi", data[4]);
                    cmd.Parameters.AddWithValue("@prescrizioni", data[5]);
                    cmd.Parameters.AddWithValue("@medico", data[6]);
                    cmd.Parameters.AddWithValue("@paziente", data[7]);
                    cmd.ExecuteNonQuery();
                    return "Visita registrata con successo";
                }
            }
            catch (Exception ex)
            {
                return "Errore durante la registrazione della visita: " + ex.Message;
            }
        }

        static string InserisciCertificato(string[] data)
        {
            if (data.Length < 6)
            {
                return "Dati insufficienti per inserire il certificato";
            }

            try
            {
                using (SqliteConnection conn = new SqliteConnection(dbPath))
                {
                    conn.Open();
                    string query = "INSERT INTO Certificato (Data, Diagnosi, Giorni, Medico, Paziente) VALUES (@data, @diagnosi, @giorni, @medico, @paziente)";
                    SqliteCommand cmd = new SqliteCommand(query, conn);
                    cmd.Parameters.AddWithValue("@data", data[1]);
                    cmd.Parameters.AddWithValue("@diagnosi", data[2]);
                    cmd.Parameters.AddWithValue("@giorni", data[3]);
                    cmd.Parameters.AddWithValue("@medico", data[4]);
                    cmd.Parameters.AddWithValue("@paziente", data[5]);
                    cmd.ExecuteNonQuery();
                    return "Certificato inserito con successo";
                }
            }
            catch (Exception ex)
            {
                return "Errore durante l'inserimento del certificato: " + ex.Message;
            }
        }
    }
}