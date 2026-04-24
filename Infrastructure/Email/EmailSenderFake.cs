namespace proyect.Infrastructure.Email;

// Este es un "cliente externo" SALIENTE: nuestro proyecto se comunica
// hacia afuera (el servidor de mail) para que pase algo en el mundo real.
//
// El nombre lleva el sufijo "Fake" porque no manda mails de verdad:
// escribe a la consola lo que MANDARIA. En produccion, el metodo
// SendWelcomeEmail abriria una conexion SMTP (por ejemplo con MailKit)
// o llamaria a un servicio como SendGrid, Amazon SES o Turbo-SMTP.
//
// Por que lo dejamos fake:
//   - Los alumnos pueden correr el proyecto sin tener credenciales de
//     mail ni miedo a spamear a nadie.
//   - Queda claro CUAL es el contrato publico de la clase (el metodo
//     SendWelcomeEmail con dos strings), sin que se pierda entre el
//     boilerplate de SMTP.
//   - El dia que se quiera mandar mails de verdad, se reemplaza la
//     implementacion de SendWelcomeEmail y nadie mas en el proyecto se
//     entera: ni el GameService, ni el Controller, ni la view.
//
// Esta clase vive en Infrastructure/ porque no es parte del dominio
// (no sabe de Games ni de Users como conceptos), es una herramienta de
// salida que el dominio usa cuando la necesita.
public class EmailSenderFake
{
    public void SendWelcomeEmail(string toAddress, string gameName)
    {
        // En produccion, en vez de Console.WriteLine habria algo como:
        //
        //     using (SmtpClient client = new SmtpClient("smtp.turbo-smtp.com"))
        //     {
        //         client.Credentials = new NetworkCredential(user, password);
        //         MailMessage message = new MailMessage(from, toAddress);
        //         message.Subject = "Tu juego '" + gameName + "' fue publicado";
        //         message.Body = "Felicitaciones...";
        //         client.Send(message);
        //     }
        //
        // Lo escribimos a consola para que el alumno vea EL EFECTO del
        // metodo sin tener que mirar una bandeja de entrada real.
        Console.WriteLine("");
        Console.WriteLine("[EmailSenderFake] --- mail simulado ---");
        Console.WriteLine("[EmailSenderFake] Para:     " + toAddress);
        Console.WriteLine("[EmailSenderFake] Asunto:   Tu juego '" + gameName + "' fue cargado en Indeura");
        Console.WriteLine("[EmailSenderFake] Cuerpo:   Felicitaciones. Cuando lo pases a publico va a aparecer en el catalogo.");
        Console.WriteLine("[EmailSenderFake] --- fin del mail simulado ---");
        Console.WriteLine("");
    }
}
