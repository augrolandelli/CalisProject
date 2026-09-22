using CalisApi.Auth;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Data;

/// <summary>
/// Semilla de datos para desarrollo: categorías, videos de muestra, rutinas
/// y usuarios de prueba. Solo se ejecuta en entorno Development.
/// Las URLs de video son placeholders públicos hasta conectar el storage real (S3).
/// </summary>
public static class DbSeeder
{
    private const string SampleVideoBase =
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/";

    public static async Task SeedAsync(CalisDbContext db, IPasswordHasher passwordHasher)
    {
        await db.Database.MigrateAsync();
        await SeedCatalogAsync(db, passwordHasher);
        await SeedSessionsAsync(db, passwordHasher);
        await SeedAchievementsAsync(db);
        await SeedCommunityAsync(db);
    }

    private static async Task SeedCommunityAsync(CalisDbContext db)
    {
        var admin = await db.Users.FirstAsync(u => u.Role == Roles.Admin);
        var clover = await db.Users.FirstAsync(u => u.Email == "clover@calisapp.com");
        if (!await db.Posts.AnyAsync())
        {
            db.Posts.Add(new Post
            {
                AuthorId = admin.Id, Title = "Bienvenidos a nuestra comunidad",
                Content = "Este es nuestro punto de encuentro: novedades del gimnasio, logros que merecen celebrarse y próximos eventos. ¡Entrenamos juntos!",
            });
            db.Posts.Add(new Post
            {
                AuthorId = admin.Id, Kind = PostKinds.Competition, Title = "Encuentro de habilidades — demostración",
                Content = "Una jornada para compartir nuestros avances en estáticos y freestyle. Consulta la organización en el gimnasio.",
                StartsAt = DateTime.UtcNow.Date.AddDays(14).AddHours(15), Location = "Gimnasio CalisApp",
            });
        }
        if (!await db.CommunityEvents.AnyAsync())
            db.CommunityEvents.Add(new CommunityEvent
            {
                Title = "Entrenamiento en equipo", Description = "Un encuentro para entrenar técnica y compartir una sesión con la comunidad.",
                Location = "Gimnasio CalisApp", StartsAt = DateTime.UtcNow.Date.AddDays(7).AddHours(15),
                EndsAt = DateTime.UtcNow.Date.AddDays(7).AddHours(17), Capacity = 20,
            });
        if (!await db.SessionReviews.AnyAsync())
        {
            var date = DateTime.UtcNow.AddDays(-1);
            var session = new Session
            {
                Title = "Clase de demostración — fundamentos", Description = "Clase pasada para probar reseñas y otorgamiento de logros en desarrollo.",
                Date = date, DurationMinutes = 60, Difficulty = "basica", CoachName = "Equipo CalisApp", LimitedSpots = 10, Enrolled = 1,
                Enrollments = [new UserSession { UserId = clover.Id, CreatedAt = date.AddHours(-2) }],
            };
            db.Sessions.Add(session);
            db.SessionReviews.Add(new SessionReview
            {
                UserId = clover.Id, Session = session, SessionTitle = session.Title, SessionDate = date,
                Rating = 5, Content = "Reseña de demostración: muy buenas progresiones y correcciones de técnica.",
            });
            var achievement = await db.Achievements.FirstAsync();
            db.SessionAchievements.Add(new SessionAchievement { Session = session, AchievementId = achievement.Id });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedCatalogAsync(CalisDbContext db, IPasswordHasher passwordHasher)
    {
        if (await db.Categories.AnyAsync())
        {
            return; // ya sembrado
        }

        var categories = new List<Category>
        {
            new() { Name = "Pull", Description = "Espalda y bíceps: dominadas, muscle-ups, remo." },
            new() { Name = "Push", Description = "Pecho y hombros: flexiones, fondas, handstand." },
            new() { Name = "Legs", Description = "Tren inferior: sentadillas, zancadas, pistol squat." },
            new() { Name = "Core", Description = "Zona media: plancha, L-sit, hollow body." },
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var pull = categories[0];
        var push = categories[1];
        var legs = categories[2];
        var core = categories[3];

        var videos = new List<Video>
        {
            new() { Title = "Dominadas estrictas", Description = "Técnica completa de dominada estricta con control escapular.", Difficulty = "intermedia", Requisites = "Colgarse 30s de la barra.", Url = SampleVideoBase + "ForBiggerBlazes.mp4", CategoryId = pull.Id },
            new() { Title = "Muscle-Up Progression", Description = "Progresión de muscle-up: jalón explosivo y transición.", Difficulty = "avanzada", Requisites = "8 dominadas estrictas y 8 fondas.", Url = SampleVideoBase + "ForBiggerEscapes.mp4", CategoryId = pull.Id },
            new() { Title = "Remo australiano", Description = "Remo invertido para construir la base de tracción.", Difficulty = "basica", Requisites = "Ninguno.", Url = SampleVideoBase + "ForBiggerFun.mp4", CategoryId = pull.Id },
            new() { Title = "Flexiones diamante", Description = "Flexiones con énfasis en tríceps y pecho interno.", Difficulty = "basica", Requisites = "10 flexiones estándar.", Url = SampleVideoBase + "ForBiggerJoyrides.mp4", CategoryId = push.Id },
            new() { Title = "Pseudo Planche Pushups", Description = "Flexiones con inclinación hacia adelante para progresar a plancha.", Difficulty = "intermedia", Requisites = "20 flexiones estándar.", Url = SampleVideoBase + "ForBiggerMeltdowns.mp4", CategoryId = push.Id },
            new() { Title = "Wall Handstand Pushups", Description = "Flexiones en parada de manos contra pared.", Difficulty = "avanzada", Requisites = "Parada de manos 30s contra pared.", Url = SampleVideoBase + "ForBiggerBlazes.mp4", CategoryId = push.Id },
            new() { Title = "Sentadilla búlgara", Description = "Fuerza unilateral de piernas con pie elevado.", Difficulty = "intermedia", Requisites = "20 sentadillas con peso corporal.", Url = SampleVideoBase + "ForBiggerEscapes.mp4", CategoryId = legs.Id },
            new() { Title = "Plancha isométrica", Description = "Plancha con activación completa del core.", Difficulty = "basica", Requisites = "Ninguno.", Url = SampleVideoBase + "ForBiggerFun.mp4", CategoryId = core.Id },
            new() { Title = "L-Sit Hold", Description = "Progresión de L-sit en paralelas.", Difficulty = "avanzada", Requisites = "Plancha 60s y fondas en paralelas.", Url = SampleVideoBase + "ForBiggerJoyrides.mp4", CategoryId = core.Id },
        };
        db.Videos.AddRange(videos);
        await db.SaveChangesAsync();

        var rutines = new List<Rutine>
        {
            new()
            {
                Title = "Intro to Pulling",
                Description = "Construye la fuerza base de tracción con remos y colgadas.",
                Duration = "25 min",
                Difficulty = "basica",
                CategoryId = pull.Id,
                Exercises =
                [
                    new() { Exercise = "Colgada activa", Tipo = ExerciseTypes.Calentamiento, Reps = 1, Series = 2, Descanso = "45s", Obs = "Escápulas activas, sin balanceo.", Order = 1 },
                    new() { Exercise = "Remo australiano", Tipo = ExerciseTypes.Principal, Reps = 10, Series = 4, Descanso = "60s", Obs = "Cuerpo recto, pecho a la barra.", Order = 2, VideoId = videos[2].Id },
                    new() { Exercise = "Dominada negativa", Tipo = ExerciseTypes.Principal, Reps = 5, Series = 3, Descanso = "90s", Obs = "Bajar en 5 segundos controlados.", Order = 3 },
                ],
            },
            new()
            {
                Title = "The Pull-Up King",
                Description = "Aumentá tus dominadas máximas con volumen progresivo.",
                Duration = "30 min",
                Difficulty = "intermedia",
                CategoryId = pull.Id,
                Exercises =
                [
                    new() { Exercise = "Rotaciones de hombro con banda", Tipo = ExerciseTypes.Calentamiento, Reps = 12, Series = 2, Descanso = "30s", Obs = "Calentar manguito rotador.", Order = 1 },
                    new() { Exercise = "Dominadas estrictas", Tipo = ExerciseTypes.Principal, Reps = 8, Series = 5, Descanso = "90s", Obs = "Rango completo, sin kipping.", Order = 2, VideoId = videos[0].Id },
                    new() { Exercise = "Muscle-Up Progression", Tipo = ExerciseTypes.Principal, Reps = 5, Series = 3, Descanso = "120s", Obs = "Jalón explosivo, transición rápida.", Order = 3, VideoId = videos[1].Id },
                ],
            },
            new()
            {
                Title = "Push Fundamentals",
                Description = "Base de empuje: flexiones, fondas y estabilidad escapular.",
                Duration = "35 min",
                Difficulty = "basica",
                CategoryId = push.Id,
                Exercises =
                [
                    new() { Exercise = "Plancha isométrica", Tipo = ExerciseTypes.Calentamiento, Reps = 1, Series = 2, Descanso = "30s", Obs = "40 segundos por serie.", Order = 1, VideoId = videos[7].Id },
                    new() { Exercise = "Flexiones diamante", Tipo = ExerciseTypes.Principal, Reps = 12, Series = 4, Descanso = "60s", Obs = "Codos pegados al torso.", Order = 2, VideoId = videos[3].Id },
                    new() { Exercise = "Pseudo Planche Pushups", Tipo = ExerciseTypes.Principal, Reps = 8, Series = 3, Descanso = "90s", Obs = "Hombros adelante de las muñecas.", Order = 3, VideoId = videos[4].Id },
                ],
            },
        };
        db.Rutines.AddRange(rutines);

        db.Users.AddRange(
            new User
            {
                FullName = "Admin CalisApp",
                Phone = "0000000000",
                Email = "admin@calisapp.com",
                PasswordHash = passwordHasher.Hash("Admin123!"),
                Role = Roles.Admin,
            },
            new User
            {
                FullName = "Guerrero Demo",
                Phone = "1111111111",
                Email = "demo@calisapp.com",
                PasswordHash = passwordHasher.Hash("Demo1234!"),
                Role = Roles.Guerrero,
            });

        await db.SaveChangesAsync();
    }

    /// <summary>Clases de la semana actual + usuario Clover de prueba.</summary>
    private static async Task SeedSessionsAsync(CalisDbContext db, IPasswordHasher passwordHasher)
    {
        if (!await db.Users.AnyAsync(u => u.Email == "clover@calisapp.com"))
        {
            db.Users.Add(new User
            {
                FullName = "Clover Demo",
                Phone = "2222222222",
                Email = "clover@calisapp.com",
                PasswordHash = passwordHasher.Hash("Clover1234!"),
                Role = Roles.Clover,
            });
            await db.SaveChangesAsync();
        }

        if (await db.Sessions.AnyAsync())
        {
            return;
        }

        var today = DateTime.UtcNow.Date;
        db.Sessions.AddRange(
            new Session
            {
                Title = "Fundamentos de Pull",
                Description = "Clase para construir la base de tracción: dominadas asistidas, remo y trabajo escapular. Ideal para quienes arrancan.",
                Date = today.AddDays(1).AddHours(10),
                LimitedSpots = 15,
                Enrolled = 2,
                Difficulty = "basica",
                CoachName = "Coach Sarah",
            },
            new Session
            {
                Title = "Advanced Static Holds",
                Description = "Plancha, front lever y back lever: progresiones avanzadas de estáticos con corrección técnica en vivo.",
                Date = today.AddDays(1).AddHours(18),
                LimitedSpots = 12,
                Enrolled = 8,
                Difficulty = "avanzada",
                CoachName = "Coach Viktor",
            },
            new Session
            {
                Title = "Ring Muscle Up Mastery",
                Description = "Transición del muscle-up en anillas, false grip y jalón explosivo. Requisito: 8 dominadas y 8 fondas estrictas.",
                Date = today.AddDays(2).AddHours(14),
                LimitedSpots = 10,
                Enrolled = 10, // clase llena: prueba la waitlist
                Difficulty = "intermedia",
                CoachName = "Coach Viktor",
            },
            new Session
            {
                Title = "Core & L-Sit Workshop",
                Description = "Hollow body, L-sit y V-sit: fuerza de zona media aplicada a los estáticos.",
                Date = today.AddDays(3).AddHours(9),
                LimitedSpots = 20,
                Enrolled = 5,
                Difficulty = "intermedia",
                CoachName = "Coach Sarah",
            });

        await db.SaveChangesAsync();
    }

    /// <summary>Logros de muestra: catálogo, asociación a clases y uno otorgado al usuario Clover demo.</summary>
    private static async Task SeedAchievementsAsync(CalisDbContext db)
    {
        if (await db.Achievements.AnyAsync())
        {
            return;
        }

        var achievements = new List<Achievement>
        {
            new() { Name = "Primera clase", Description = "Completaste tu primera clase en vivo.", Icon = "🎯" },
            new() { Name = "Racha 7 días", Description = "Entrenaste 7 días seguidos.", Icon = "🔥" },
            new() { Name = "First Muscle-Up", Description = "Lograste tu primer muscle-up.", Icon = "🏅" },
            new() { Name = "Tuck Planche", Description = "Manteniste tuck planche por 10 segundos.", Icon = "⭐" },
            new() { Name = "Pro Form", Description = "Técnica perfecta destacada por el coach.", Icon = "💎" },
        };
        db.Achievements.AddRange(achievements);
        await db.SaveChangesAsync();

        // Logros que se pueden ganar en clases específicas (spec §3.6)
        var muscleUpClass = await db.Sessions.FirstAsync(s => s.Title == "Ring Muscle Up Mastery");
        var holdsClass = await db.Sessions.FirstAsync(s => s.Title == "Advanced Static Holds");

        db.SessionAchievements.AddRange(
            new SessionAchievement { SessionId = muscleUpClass.Id, AchievementId = achievements[2].Id },
            new SessionAchievement { SessionId = holdsClass.Id, AchievementId = achievements[3].Id },
            new SessionAchievement { SessionId = holdsClass.Id, AchievementId = achievements[4].Id });

        // Un logro ya otorgado al usuario Clover demo
        var clover = await db.Users.FirstOrDefaultAsync(u => u.Email == "clover@calisapp.com");
        if (clover is not null)
        {
            db.UserAchievements.Add(new UserAchievement
            {
                UserId = clover.Id,
                AchievementId = achievements[0].Id,
                SessionId = null,
            });
        }

        await db.SaveChangesAsync();
    }
}
