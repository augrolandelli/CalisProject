using CalisApi.Models;
using FluentValidation;

namespace CalisApi.Dtos.Validators;

/// <summary>Validadores del panel Admin (Fase 5).</summary>
public class WriteCategoryValidator : AbstractValidator<WriteCategoryRequest>
{
    public WriteCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("El nombre es obligatorio (máximo 100 caracteres).");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500).WithMessage("La descripción es obligatoria (máximo 500 caracteres).");
    }
}

public class WriteVideoValidator : AbstractValidator<WriteVideoRequest>
{
    public WriteVideoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Difficulty).Must(d => d is "basica" or "intermedia" or "avanzada")
            .WithMessage("Dificultad inválida (basica, intermedia o avanzada).");
        RuleFor(x => x.Requisites).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
}

public class WriteRutineValidator : AbstractValidator<WriteRutineRequest>
{
    public WriteRutineValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Duration).NotEmpty().MaximumLength(50).WithMessage("Indica la duración aproximada (ej. \"45 min\").");
        RuleFor(x => x.Difficulty).Must(d => d is "basica" or "intermedia" or "avanzada")
            .WithMessage("Dificultad inválida (basica, intermedia o avanzada).");
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Exercises).NotEmpty().Must(e => e.Length <= 50)
            .WithMessage("La rutina necesita al menos un ejercicio (máximo 50).");
        RuleForEach(x => x.Exercises).ChildRules(exercise =>
        {
            exercise.RuleFor(e => e.Exercise).NotEmpty().MaximumLength(200);
            exercise.RuleFor(e => e.Tipo).Must(t => t is ExerciseTypes.Calentamiento or ExerciseTypes.Principal)
                .WithMessage("Tipo inválido (Calentamiento o Principal).");
            exercise.RuleFor(e => e.Reps).InclusiveBetween(1, 1000);
            exercise.RuleFor(e => e.Series).InclusiveBetween(1, 50);
            exercise.RuleFor(e => e.Descanso).NotEmpty().MaximumLength(20);
            exercise.RuleFor(e => e.Obs).NotEmpty().MaximumLength(500).WithMessage("Cada ejercicio necesita observaciones (máximo 500 caracteres).");
        });
    }
}
