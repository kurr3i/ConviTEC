using ConviTEC.Algoritmos;

namespace ConviTEC.Tests
{
    public class MergeSortTests
    {
        private static int Ascendente(int a, int b) => a.CompareTo(b);

        [Fact]
        public void ArregloVacio_NoFalla()
        {
            int[] arreglo = { };

            MergeSort.Ordenar(arreglo, Ascendente);

            Assert.Empty(arreglo);
        }

        [Fact]
        public void UnSoloElemento_QuedaIgual()
        {
            int[] arreglo = { 7 };

            MergeSort.Ordenar(arreglo, Ascendente);

            Assert.Equal(new[] { 7 }, arreglo);
        }

        [Fact]
        public void Desordenado_QuedaOrdenado()
        {
            int[] arreglo = { 38, 27, 43, 3, 9, 82, 10 };

            MergeSort.Ordenar(arreglo, Ascendente);

            Assert.Equal(new[] { 3, 9, 10, 27, 38, 43, 82 }, arreglo);
        }

        [Fact]
        public void YaOrdenado_QuedaIgual()
        {
            int[] arreglo = { 1, 2, 3, 4, 5 };

            MergeSort.Ordenar(arreglo, Ascendente);

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, arreglo);
        }

        [Fact]
        public void OrdenInverso_QuedaOrdenado()
        {
            int[] arreglo = { 5, 4, 3, 2, 1 };

            MergeSort.Ordenar(arreglo, Ascendente);

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, arreglo);
        }

        [Fact]
        public void ConDuplicados_NoPierdeNinguno()
        {
            int[] arreglo = { 4, 1, 4, 2, 1, 4 };

            MergeSort.Ordenar(arreglo, Ascendente);

            Assert.Equal(new[] { 1, 1, 2, 4, 4, 4 }, arreglo);
        }

        [Fact]
        public void CriterioDescendente_OrdenaDeMayorAMenor()
        {
            int[] arreglo = { 3, 9, 1, 7 };

            MergeSort.Ordenar(arreglo, (a, b) => b.CompareTo(a));

            Assert.Equal(new[] { 9, 7, 3, 1 }, arreglo);
        }

        [Fact]
        public void Strings_OrdenaAlfabeticamente()
        {
            string[] arreglo = { "P003", "P001", "P010", "P002" };

            MergeSort.Ordenar(arreglo, (a, b) => string.CompareOrdinal(a, b));

            Assert.Equal(new[] { "P001", "P002", "P003", "P010" }, arreglo);
        }

        [Fact]
        public void EsEstable_LosEmpatesConservanSuOrden()
        {
            Item[] arreglo =
            {
                new Item("P-001", 5.0),
                new Item("P-002", 2.0),
                new Item("P-003", 5.0),
                new Item("P-004", 2.0),
                new Item("P-005", 5.0)
            };

            MergeSort.Ordenar(arreglo, (a, b) => a.Rendimiento.CompareTo(b.Rendimiento));

            string[] ids = new string[arreglo.Length];
            for (int i = 0; i < arreglo.Length; i++)
                ids[i] = arreglo[i].Id;

            Assert.Equal(new[] { "P-002", "P-004", "P-001", "P-003", "P-005" }, ids);
        }

        [Fact]
        public void ArregloNull_Lanza()
        {
            Assert.Throws<ArgumentNullException>(() => MergeSort.Ordenar<int>(null!, Ascendente));
        }

        [Fact]
        public void ComparadorNull_Lanza()
        {
            int[] arreglo = { 2, 1 };

            Assert.Throws<ArgumentNullException>(() => MergeSort.Ordenar(arreglo, null!));
        }

        [Fact]
        public void MilElementosAleatorios_QuedanOrdenados()
        {
            var azar = new Random(7);
            int[] arreglo = new int[1000];
            int suma = 0;
            for (int i = 0; i < arreglo.Length; i++)
            {
                arreglo[i] = azar.Next(-500, 500);
                suma += arreglo[i];
            }

            MergeSort.Ordenar(arreglo, Ascendente);

            int sumaDespues = 0;
            for (int i = 0; i < arreglo.Length; i++)
            {
                sumaDespues += arreglo[i];
                if (i > 0)
                    Assert.True(arreglo[i - 1] <= arreglo[i], $"Desorden en la posición {i}.");
            }
            Assert.Equal(suma, sumaDespues);
        }

        private record Item(string Id, double Rendimiento);
    }
}
