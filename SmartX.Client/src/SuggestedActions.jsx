import { useCallback, useEffect, useRef, useState } from "react";

const API = "http://localhost:5206/api";

async function request(path, options = {}) {
    const response = await fetch(`${API}/${path}`, options);
    const data = await response.json();

    if (!response.ok)
        throw new Error(data.message || `Request failed (${response.status}).`);

    return data;
}

const actionKey = action => [
    action.sourceSensorId,
    action.faultType,
    action.targetSensorId,
    action.targetState
].join("|");

export default function SuggestedActions() {
    const [suggestions, setSuggestions] = useState([]);
    const [loaded, setLoaded] = useState(false);
    const [loadError, setLoadError] = useState("");
    const [actionError, setActionError] = useState("");
    const [message, setMessage] = useState("");
    const [busyKey, setBusyKey] = useState("");

    const busyRef = useRef(false);
    const requestVersion = useRef(0);

    const refresh = useCallback(async signal => {
        const version = ++requestVersion.current;

        try {
            const data = await request("Recommendation", { signal });

            if (!signal?.aborted && version === requestVersion.current) {
                setSuggestions(data);
                setLoaded(true);
                setLoadError("");
            }
        } catch (err) {
            if (!signal?.aborted && version === requestVersion.current)
                setLoadError(err.message || "Cannot load suggested actions.");
        }
    }, []);

    useEffect(() => {
        const controller = new AbortController();
        let timer;

        async function poll() {
            await refresh(controller.signal);
            if (!controller.signal.aborted)
                timer = window.setTimeout(poll, 2000);
        }

        poll();

        return () => {
            controller.abort();
            window.clearTimeout(timer);
            requestVersion.current++;
        };
    }, [refresh]);

    async function apply(action) {
        if (busyRef.current) return;

        busyRef.current = true;
        setBusyKey(actionKey(action));
        setActionError("");
        setMessage("");

        try {
            // Recheck relevance before sending the operator's command.
            const current = await request("Recommendation");

            const stillRelevant = current.some(candidate =>
                actionKey(candidate) === actionKey(action)
            );

            if (!stillRelevant) {
                throw new Error(
                    "Conditions have changed. This suggestion is no longer available."
                );
            }

            const result = await request("Command", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    sensorId: action.targetSensorId,
                    state: action.targetState
                })
            });

            setMessage(
                `${result.message} You can undo it in Actuator Commands.`
            );
        } catch (err) {
            setActionError(err.message || "Suggested action could not be applied.");
        } finally {
            await refresh();
            busyRef.current = false;
            setBusyKey("");
        }
    }

    return (
        <section className="sx-panel" aria-labelledby="suggestions-title">
            <h2 id="suggestions-title">Suggested Actions</h2>
            <p className="sx-muted">
                Recommendations learned from device searches and manual commands.
                Review the device and explanation before applying.
            </p>

            {loadError && <p className="sx-error" role="alert">{loadError}</p>}
            {actionError && <p className="sx-error" role="alert">{actionError}</p>}
            <p className="sx-feedback" role="status">{message}</p>

            {!loaded && !loadError && (
                <p role="status">Checking for relevant actions…</p>
            )}

            {loaded && !loadError && suggestions.length === 0 && (
                <p className="sx-empty">
                    No relevant learned actions right now. Suggestions appear
                    after at least three matching search-command observations
                    when the associated fault is active.
                </p>
            )}

            <div className="sx-suggestion-grid">
                {suggestions.map(action => {
                    const key = actionKey(action);
                    const desiredState = action.targetState ? "ON" : "OFF";

                    return (
                        <article className="sx-suggestion" key={key}>
                            <p className="sx-eyebrow">
                                LEARNED FROM {action.support} OBSERVATIONS
                            </p>
                            <h3>
                                Set {action.targetLocation} {desiredState}
                            </h3>
                            <p>
                                <strong>Source:</strong> {action.sourceLocation}
                            </p>
                            <p className="sx-muted">{action.faultType}</p>
                            <p className="sx-explanation">{action.explanation}</p>
                            <p className="sx-muted">
                                Target device: {action.targetSensorId}
                            </p>
                            <button type="button"
                                disabled={!!busyKey || !!loadError}
                                onClick={() => apply(action)}>
                                {busyKey === key
                                    ? "Applying…"
                                    : `Apply ${desiredState} command`}
                            </button>
                        </article>
                    );
                })}
            </div>
        </section>
    );
}
