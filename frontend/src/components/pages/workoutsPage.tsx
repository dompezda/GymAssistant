import { useNavigate } from "react-router-dom";


export default function WorkoutsPage() {
  const navigate = useNavigate();

  return (
    <section>
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć
      </button>

      <h1>Treningi</h1>
      <p>Tutaj będzie lista treningów.</p>
    </section>
  );
}