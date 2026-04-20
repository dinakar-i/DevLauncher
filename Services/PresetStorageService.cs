using System.IO;
using DevLauncher.Models;
using Newtonsoft.Json;

namespace DevLauncher.Services;

public class PresetStorageService
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevLauncher");

    private static readonly string PresetsFile =
        Path.Combine(AppDataDir, "presets.json");

    public List<LaunchPreset> LoadCustomPresets()
    {
        try
        {
            if (!File.Exists(PresetsFile))  SaveCustomPresets(GetBuiltInPresets());
            var json = File.ReadAllText(PresetsFile);
            return JsonConvert.DeserializeObject<List<LaunchPreset>>(json) ?? new();
        }
        catch { return new(); }
    }

    public void SaveCustomPresets(IEnumerable<LaunchPreset> presets)
    {
        try
        {
            Directory.CreateDirectory(AppDataDir);
            var json = JsonConvert.SerializeObject(presets.Where(p => p.IsCustom), Formatting.Indented);
            File.WriteAllText(PresetsFile, json);
        }
        catch { }
    }

    public List<LaunchPreset> GetBuiltInPresets()
    {
        return new List<LaunchPreset>
        {
            new()
            {
                Name = "Node.js Full Stack",
                Icon = "🚀", Category = "Dev",
                Description = "Install deps, start frontend + backend servers, open browser.",
                Commands =
                {
                    new() { Command = "npm install",          Description = "Installing packages" },
                    new() { Command = "npm run dev",          Description = "Starting Vite dev server", RunInBackground = true },
                    new() { Command = "npm run server",       Description = "Starting API server",      RunInBackground = true },
                },
                IsCustom=true,
                OpenUrlAfterLaunch = "http://localhost:3000"
            },
            new()
            {
                Name = "Python Django / Flask",
                Icon = "🐍", Category = "Dev",
                Description = "Activate venv, install deps, migrate DB, run dev server.",
                Commands =
                {
                    new() { Command = "pip install -r requirements.txt", Description = "Installing requirements" },
                    new() { Command = "python manage.py migrate",        Description = "Running migrations" },
                    new() { Command = "python manage.py runserver",      Description = "Starting server", RunInBackground = true },
                },
                IsCustom=true,
                OpenUrlAfterLaunch = "http://127.0.0.1:8000"
            },
            new()
            {
                Name = "Docker Compose Stack",
                Icon = "🐳", Category = "DevOps",
                Description = "Pull latest images, spin up all services, tail logs.",
                Commands =
                {
                    new() { Command = "docker-compose pull",      Description = "Pulling latest images" },
                    new() { Command = "docker-compose up -d",     Description = "Starting services" },
                    new() { Command = "docker-compose logs -f",   Description = "Tailing logs", RunInBackground = true },
                },
                IsCustom=true,
            },
            new()
            {
                Name = "Kubernetes / Minikube",
                Icon = "☸", Category = "DevOps",
                Description = "Start cluster, apply manifests, watch pods.",
                Commands =
                {
                    new() { Command = "minikube start",           Description = "Starting cluster" },
                    new() { Command = "kubectl apply -f ./k8s/",  Description = "Applying manifests" },
                    new() { Command = "kubectl get pods -w",      Description = "Watching pods", RunInBackground = true },
                },
                IsCustom=true,
            },
            new()
            {
                Name = "React + Vite",
                Icon = "⚛", Category = "Dev",
                Description = "npm ci, start Vite, run Storybook.",
                Commands =
                {
                    new() { Command = "npm ci",                           Description = "Clean install" },
                    new() { Command = "npx vite",                         Description = "Starting Vite", RunInBackground = true },
                    new() { Command = "npx storybook dev -p 6006",        Description = "Starting Storybook", RunInBackground = true },
                },
                OpenUrlAfterLaunch = "http://localhost:5173",
                IsCustom=true
            },
            new()
            {
                Name = "Jupyter Data Science",
                Icon = "📊", Category = "Data",
                Description = "Activate conda env, start Jupyter Lab and MLflow.",
                Commands =
                {
                    new() { Command = "jupyter lab",      Description = "Starting JupyterLab", RunInBackground = true },
                    new() { Command = "mlflow ui",        Description = "Starting MLflow UI",  RunInBackground = true },
                },
                OpenUrlAfterLaunch = "http://localhost:8888/lab",
                IsCustom=true
            },
            new()
            {
                Name = "CI/CD Pipeline Check",
                Icon = "🔁", Category = "DevOps",
                Description = "Lint → Test → Build → Push to staging.",
                Commands =
                {
                    new() { Command = "npm run lint",             Description = "Running linter" },
                    new() { Command = "npm test -- --coverage",   Description = "Running tests" },
                    new() { Command = "npm run build",            Description = "Building for production" },
                    new() { Command = "git push origin staging",  Description = "Pushing to staging" },
                },
                IsCustom=true
            },
            new()
            {
                Name = "DB + Redis Local Stack",
                Icon = "🗄", Category = "Data",
                Description = "Start PostgreSQL, Redis, run migrations.",
                Commands =
                {
                    new() { Command = "redis-server --daemonize yes",     Description = "Starting Redis" },
                    new() { Command = "npx prisma migrate dev",           Description = "Running Prisma migrations" },
                },
                IsCustom=true,
            },
        };
    }
}
