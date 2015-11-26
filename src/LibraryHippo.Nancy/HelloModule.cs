namespace LibraryHippo.Nancy
{
    using global::Nancy;

    public class HelloModule : NancyModule
    {
        public HelloModule()
        {
            this.Get["/"] = parameters => "Hello World";
        }
    }
}