import { useNavigate } from 'react-router-dom';
import '../../styles/global.css';
import useAuth from '../../auth/useAuth';

export default function MainMenu() {
  const navigate = useNavigate();
  const {logout} = useAuth();

  return (
    <section className="page main-menu">
      <h1>Gym Assistant</h1>

      <button className="btn btn-primary" onClick={() => navigate("/exercises")}>Ćwiczenia</button>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-templates")}>Plany Treningowe</button>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-sessions/start")}>Rozpocznij trening</button>
      <button className="btn btn-secondary" onClick={() => navigate("/workout-sessions")}>Historia treningów</button>
      <button className="btn btn-secondary" onClick={() => navigate("/settings")}>Ustawienia</button>
      <button className="btn btn-secondary" onClick={logout}>Wyloguj</button>
    </section>
  );
}