# Push SmartReconcile AI to GitHub

The repository already exists at `kakavn87/SmartReconcile-AI`.

From this folder:

```powershell
git init
git branch -M main
git remote add origin https://github.com/kakavn87/SmartReconcile-AI.git
git add .
git commit -m "feat: initial SmartReconcile AI backend"
git push -u origin main
```

If `origin` already exists:

```powershell
git remote set-url origin https://github.com/kakavn87/SmartReconcile-AI.git
git push -u origin main
```
