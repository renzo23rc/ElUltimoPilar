/// <summary>
/// Defines a target whose speed can be reduced by timed slowdowns owned by a source.
/// </summary>
public interface ISlowable
{
    /// <summary>Applies or refreshes the slowdown owned by a source.</summary>
    /// <param name="fuente">The owner of the slowdown, such as a zone or an ability.</param>
    /// <param name="factor">The speed multiplier between 0 and 1.</param>
    /// <param name="duracion">The duration in seconds.</param>
    void AplicarRalentizacion(object fuente, float factor, float duracion);

    /// <summary>Removes only the slowdown owned by a source, keeping the others.</summary>
    /// <param name="fuente">The owner of the slowdown.</param>
    void QuitarRalentizacion(object fuente);
}
