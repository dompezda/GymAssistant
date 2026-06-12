import { useNavigate, useParams } from "react-router-dom";

export default function WorkoutSessionPage() {
  const navigate = useNavigate();
  const { id } = useParams();

  return (
    <section>
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć
      </button>

      <h1>Sesja treningowa</h1>
      <p>Sesja została rozpoczęta.</p>
      <p>ID sesji: {id}</p>
    </section>
  );
}