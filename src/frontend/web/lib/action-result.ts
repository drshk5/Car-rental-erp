export type ActionResult =
  | { ok: true; message: string }
  | { ok: false; message: string };

export function toActionFailure(error: unknown, fallbackMessage: string): ActionResult {
  return {
    ok: false,
    message: error instanceof Error ? error.message : fallbackMessage,
  };
}
