# Diamono — règles pour agents de développement

1. Lire `docs/01-discovery`, `docs/02-product`, puis `docs/03-architecture` avant toute modification.
2. Ne pas inventer de règle métier silencieusement. Si nécessaire, documenter l'hypothèse dans la story.
3. Ne pas introduire de microservice dans le MVP.
4. Ne pas hardcoder de nouvelle règle commerciale dans un composant Razor.
5. Toute opération de réservation doit rester sûre en cas de concurrence; avant production, privilégier une garantie DB.
6. Ajouter/adapter les tests pour chaque story.
7. Préserver l'expérience mobile-first et le français/FCFA du prototype.
