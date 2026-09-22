namespace CalisApi.Dtos;

/// <summary>Clave pública VAPID para suscribirse desde el navegador.</summary>
public record VapidPublicKeyResponse(string PublicKey);

/// <summary>Suscripción push enviada por el navegador (PushSubscription JSON).</summary>
public record PushSubscriptionRequest(string Endpoint, PushSubscriptionKeys Keys);

/// <summary>Claves criptográficas de la suscripción push.</summary>
public record PushSubscriptionKeys(string P256dh, string Auth);
