import { useNavigate } from "react-router-dom";

export default function SettingsPage() {

  const navigate = useNavigate();
  return (
    <section className="page">
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć
      </button>
      <h1>Ustawienia</h1>
      <p>Tutaj będą ustawienia aplikacji.</p>
    </section>
  );
}