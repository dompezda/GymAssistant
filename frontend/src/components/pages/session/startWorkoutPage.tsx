import { useNavigate } from "react-router-dom";

export default function StartWorkoutPage() {

  const navigate = useNavigate();
  return (
    <section className="page main-menu">
      <button className="btn btn-secondary" onClick={() => navigate("/")}>
        ← Wróć
      </button>
      <h1>Sesja treningowa!</h1>
      <p>W jakim trybie rozpocząc trening?</p>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-sessions/select-template")}>Wybierz plan treningowy</button>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-sessions/setup")}>Pusty trening</button>
    </section>
  );
}