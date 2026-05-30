import { useNavigate } from 'react-router-dom';
import '../../styles/global.css';


export default function MainMenu() {
  const navigate = useNavigate();

  return (
    <section className="main-menu">
      <h1>Gym Assistant</h1>

      <button className="btn btn-primary" onClick={() => navigate("/exercises")}>Ćwiczenia</button>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-templates")}>Plany Treningowe</button>
      <button className="btn btn-secondary" onClick={() => navigate("/settings")}>Ustawienia</button>
    </section>
  );
}