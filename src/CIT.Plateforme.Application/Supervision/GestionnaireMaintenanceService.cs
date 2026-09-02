namespace CIT.Plateforme.Application.Supervision;

public class GestionnaireMaintenanceService
{
    private volatile bool _estEnMaintenance;
    private string? _motif;
    private DateTime? _debutUtc;

    public bool EstEnMaintenance => _estEnMaintenance;
    public string? Motif => _motif;
    public DateTime? DebutUtc => _debutUtc;

    public void ActiverMaintenance(string motif)
    {
        _motif = motif;
        _debutUtc = DateTime.UtcNow;
        _estEnMaintenance = true;
    }

    public void DesactiverMaintenance()
    {
        _estEnMaintenance = false;
        _motif = null;
        _debutUtc = null;
    }
}
