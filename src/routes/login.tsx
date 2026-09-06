import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/auth/auth-context";
import { FieldError } from "@/components/common/ValidationSummary";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiError } from "@/lib/api/client";

export const Route = createFileRoute("/login")({
  validateSearch: (search: Record<string, unknown>) => ({
    redirect: typeof search["redirect"] === "string" ? (search["redirect"] as string) : undefined,
  }),
  head: () => ({
    meta: [
      { title: "Sign in — Enterprise Task Management" },
      {
        name: "description",
        content:
          "Sign in to the Enterprise Task Management workspace to review projects, tasks, people and departments.",
      },
      { property: "og:title", content: "Sign in — Enterprise Task Management" },
      {
        property: "og:description",
        content: "Secure sign-in for the Enterprise Task Management workspace.",
      },
    ],
  }),
  component: LoginPage,
});

/** Only in-app paths are accepted as a post-login destination. */
function safeRedirect(target: string | undefined): string {
  if (!target || !target.startsWith("/") || target.startsWith("//")) return "/dashboard";
  if (target.startsWith("/login")) return "/dashboard";
  return target;
}

function LoginPage() {
  const { login, status } = useAuth();
  const navigate = useNavigate();
  const search = Route.useSearch();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<{ email?: string; password?: string }>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const destination = safeRedirect(search.redirect);

  // Already signed in (or just signed in) — leave the sign-in screen.
  useEffect(() => {
    if (status === "authenticated") {
      void navigate({ to: destination, replace: true });
    }
  }, [destination, navigate, status]);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    const nextErrors: { email?: string; password?: string } = {};
    if (!email.trim()) nextErrors.email = "Email is required.";
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim()))
      nextErrors.email = "Enter a valid email address.";
    if (!password) nextErrors.password = "Password is required.";
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    setSubmitting(true);
    try {
      await login({ email: email.trim(), password });
      setPassword("");
      toast.success("Signed in");
      void navigate({ to: destination, replace: true });
    } catch (error) {
      const message =
        error instanceof ApiError
          ? error.status === 401
            ? "Email or password is incorrect."
            : error.message
          : "Sign in failed. Please try again.";
      setFormError(message);
      toast.error(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 px-4 py-12">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <h1 className="text-xl font-semibold tracking-tight text-foreground">Sign in</h1>
          <CardDescription>
            Use your work email to access the Enterprise Task Management workspace.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit} noValidate>
            <div>
              <Label htmlFor="email">Work email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                aria-invalid={Boolean(errors.email)}
                aria-describedby={errors.email ? "email-error" : undefined}
                className="mt-1.5"
              />
              <FieldError id="email-error" message={errors.email} />
            </div>
            <div>
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                aria-invalid={Boolean(errors.password)}
                aria-describedby={errors.password ? "password-error" : undefined}
                className="mt-1.5"
              />
              <FieldError id="password-error" message={errors.password} />
            </div>
            {formError ? (
              <p role="alert" className="text-sm text-destructive">
                {formError}
              </p>
            ) : null}
            <Button type="submit" disabled={submitting}>
              {submitting ? "Signing in…" : "Sign in"}
            </Button>
            <p className="text-center text-xs text-muted-foreground">
              Your credentials are sent directly to the Enterprise Task Management API over HTTPS.
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
