import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { fetchApi } from "../../../api/fetchApi";

export default function StartWorkoutPage() {

  const [loadError, setLoadError] = useState<string | null>(null);
  const [discardError, setDiscardError] = useState<string | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [discard, setDiscard] = useState<boolean>(false);
  const discarding = useRef<boolean>(false);

async function getActiveSession() : Promise<void> {
  setSessionId(null);
  setLoadError(null);
  try { 
      const res = await fetchApi(`/api/workout-sessions/active`, {
              method: "GET"
            });
      if(res.ok) 
      {
        if(res.status === 204) // brak aktywnej sesji
        {
          setSessionId(null);
          return;
        }
        if(res.status === 200) // aktywna sesja
        {
          const data = await res.json();
          setSessionId(data);
        }
      }
      if(!res.ok)
      {
        setLoadError(`Błąd podczas pobierania aktywnej sesji. ${res.statusText}`);
        setSessionId(null);
      }
    }
    catch(error)
    {
      setLoadError(`Błąd podczas pobierania aktywnej sesji. ${error}`);
    }
    finally {
      setIsLoading(false);
    }
  }
  useEffect(() => {
    async function loadSessions() {
        await getActiveSession();
    }
    loadSessions();
    }, []);

  const navigate = useNavigate();

    async function discardSession(): Promise<void> {
      setDiscardError(null)
      if(discarding.current || !sessionId) return;

      const confirmed = window.confirm(
        "Czy na pewno chcesz porzucić aktualny trening?"
      );

      if (!confirmed) return;
      discarding.current = true;
      setDiscard(true);
      try {
      const res =await fetchApi(`/api/workout-sessions/${sessionId}/discard`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        }
      });

      if(res.ok)
      {
        setSessionId(null);
      }
      if(!res.ok)
      {
        setDiscardError(`Błąd podczas odrzucania sesji. ${res.statusText}`);
      }
    }
    catch(error)
    {
      setDiscardError(`Błąd podczas odrzucania sesji. ${error}`);
    }
    finally {
      discarding.current = false;
      setDiscard(false);
    }
  }

  if(isLoading)
  {
    return <div>Ładowanie...</div>
  }
  if(!isLoading && loadError)
  {
    return (
      <section className="page main-menu">
        <div>{loadError}</div>
        <button className="btn btn-secondary" onClick={() => 
          {
            setIsLoading(true);  
            setLoadError(null);
            getActiveSession()}}>Spróbuj ponownie</button>
      </section>

    ) 
  }

  if(sessionId)
  {
    return (
       <section className="page main-menu">
        <p>Znaleziono aktywną sesję treningową. Czy chcesz ją kontynuować?</p>
        {discardError && <p className="error">{discardError}</p>}
          <button className="btn btn-primary" disabled={discard} onClick={() => navigate(`/workout-sessions/${sessionId}`)}>Kontynuuj</button>
          <button className="btn btn-secondary" onClick={() => discardSession()} disabled={discard}>
            Porzuć trening i rozpocznij nowy
          </button>
       </section>
    );
  }
  return (
    <section className="page stack templates-page">
       <div className="page-header">
          <button className="btn back-button" onClick={() => navigate("/")}>
            ← Wróć
          </button>
          <header>
            <h1 className="page-title">Sesja treningowa</h1>
          </header>
       </div>
       <div className="stack">
        <p>W jakim trybie rozpocząć trening?</p>
        <button className="btn btn-secondary" onClick={() => navigate("/workout-sessions/select-template")}>Wybierz plan treningowy</button>
        <button className="btn btn-secondary" onClick={() => navigate("/workout-sessions/setup")}>Pusty trening</button>
    
       </div>
    </section>
  );
}