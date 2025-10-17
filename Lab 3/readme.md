What happens if you apply pagination after materializing the query with .ToList()? 

.ToList() aduce toata tabela din baza de date in memoria aplicatiei. Paginarea se excuta apoi in memoria aplicatiei, pe lista uriasa de date deja incarcata.

Why is this problematic for large datasets?

1. Consum masiv de memorie
2. Incarcare uriasa a bazei de date si a retelei
3. Timp de raspuns ineficient
4. Blocarea serverului
