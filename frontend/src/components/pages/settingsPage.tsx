import { useNavigate } from "react-router-dom";

export default function SettingsPage() {

  const navigate = useNavigate();
  return (
    <section className="page stack templates-page">
      <div className="page-header">
          <button className="btn back-button" onClick={() => navigate("/")}>
            ← Wróć
          </button>
          <header>
            <h1 className="page-title">Ustawienia</h1>
          </header>
       </div>
      <p>Tutaj będą ustawienia aplikacji.</p>
    </section>
  );
}