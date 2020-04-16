namespace Remembrance.Contracts.DAL.Model
{
    public abstract class Entity
    {
        public int Id { get; set; }

        public bool IsNew()
        {
            return Id == 0;
        }
    }
}
