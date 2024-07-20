import { createFileRoute } from "@tanstack/react-router";
import LoginForm from "./-components/LoginForm";
import { HeroImage } from "@/shared/ui/images/HeroImage";

export const Route = createFileRoute("/login/")({
  component: LoginPage
});

export default function LoginPage() {
  return (
    <main className="flex min-h-screen flex-col">
      <div className="flex grow flex-col gap-4 md:flex-row">
        <div className="flex flex-col items-center justify-center gap-6 md:w-1/2 p-6">
          <LoginForm />
        </div>
        <div className="flex items-center justify-center p-6 bg-gray-50 md:w-1/2 md:px-28 md:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
