namespace PeliculasAPI.Entidades
{
    /*
     * Para que En el controlador customizado que hemos creado sepa que las entidades que 
     * vamos a usar para hacerle las peticiones tienen Id, creamos esta interfaz (que hay que 
     * implementar en todas las entidades que queramos) para que pueda usarlo 
     */
    public interface IId
    {
        public int Id { get; set; }
    }
}
