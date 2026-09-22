using CalisApi.Models;
using FluentValidation;

namespace CalisApi.Dtos.Validators;

public class WritePostValidator : AbstractValidator<WritePostRequest>
{
    public WritePostValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("El título es obligatorio (máximo 200 caracteres).");
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000).WithMessage("Escribe el contenido (máximo 5000 caracteres).");
        RuleFor(x => x.Kind).Must(k => k is PostKinds.Announcement or PostKinds.Competition).WithMessage("Tipo de publicación inválido.");
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.StartsAt).NotNull().When(x => x.Kind == PostKinds.Competition).WithMessage("Indica la fecha de la competencia.");
        RuleFor(x => x.Location).NotEmpty().When(x => x.Kind == PostKinds.Competition).WithMessage("Indica el lugar de la competencia.");
        RuleFor(x => x.MediaIds).NotNull().Must(ids => ids is { Length: <= 5 } && ids.Distinct().Count() == ids.Length)
            .WithMessage("Puedes adjuntar hasta 4 fotos y un video, sin duplicados.");
    }
}

public class WriteReviewValidator : AbstractValidator<WriteReviewRequest>
{
    public WriteReviewValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Elige entre 1 y 5 estrellas.");
        RuleFor(x => x.Content).MaximumLength(1500).WithMessage("La reseña admite hasta 1500 caracteres.");
    }
}

public class WriteEventValidator : AbstractValidator<WriteEventRequest>
{
    public WriteEventValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(300);
        RuleFor(x => x.StartsAt).Must(d => d.Kind == DateTimeKind.Utc && d > DateTime.UtcNow)
            .WithMessage("El inicio debe ser una fecha futura con zona horaria.");
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt).WithMessage("El final debe ser posterior al inicio.");
        RuleFor(x => x.Capacity).InclusiveBetween(1, 10000).When(x => x.Capacity.HasValue);
    }
}

public class BeginUploadValidator : AbstractValidator<BeginUploadRequest>
{
    public BeginUploadValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Purpose).Must(p => p is "community" or "exercise").WithMessage("Finalidad de subida inválida.");
        RuleFor(x => x.ContentType).Must(t => t is "image/jpeg" or "image/png" or "image/webp" or "video/mp4")
            .WithMessage("Usa fotos JPG, PNG o WebP, o videos MP4 (H.264/AAC).");
        RuleFor(x => x.ContentType).Must(t => t == "video/mp4").When(x => x.Purpose == "exercise")
            .WithMessage("Los videos de ejercicios deben ser MP4 (H.264/AAC).");
        RuleFor(x => x.Size).GreaterThan(0)
            .LessThanOrEqualTo(x => x.Purpose == "exercise" ? 50 * 1024 * 1024
                : x.ContentType == "video/mp4" ? 100 * 1024 * 1024 : 10 * 1024 * 1024)
            .WithMessage("Máximo 10 MB por foto, 100 MB por clip de comunidad o 50 MB por video de ejercicio.");
    }
}
