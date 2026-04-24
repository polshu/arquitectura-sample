using proyect.Infrastructure.Persistence.Repositories;

namespace proyect.Services;

public class UserService
{
    // Se podria inicializar el campo directamente en la declaracion:
    //     private readonly UserRepository _userRepository = new UserRepository();
    // Lo hacemos en el constructor para que quede bien visible donde se crea.
    private readonly UserRepository _userRepository;

    public UserService()
    {
        _userRepository = new UserRepository();
    }

    public void Insert(User user)
    {
        _userRepository.Insert(user);
    }

    public void Update(User user)
    {
        _userRepository.Update(user);
    }

    public User? GetById(int id)
    {
        return _userRepository.GetById(id);
    }

    public User? GetByUserName(string userName)
    {
        return _userRepository.GetByUserName(userName);
    }

    public User? GetByEmail(string email)
    {
        return _userRepository.GetByEmail(email);
    }

    // Reglas simples expresadas como metodos claros, en vez de que el
    // Controller tenga que saber que significa "COUNT(*) > 0".
    // Compara este metodo con UserNameAlreadyUsed (justo abajo):
    // ese otro reusa GetByUserName, pero este mantiene su propia query
    // (CountByEmail) en el Repository.
    //
    // Este se podria reescribir igual, reusando GetByEmail:
    //     User? existingUser = _userRepository.GetByEmail(email);
    //     return existingUser != null;
    // y asi borrar CountByEmail del Repository.
    //
    // Lo dejamos como esta a proposito, para que vean los dos caminos.
    // Cual te parece mejor? Algunas pistas para pensarlo:
    //   - Con GetByEmail reusamos codigo y mantenemos una query menos.
    //   - Con CountByEmail la base solo cuenta filas: no trae todas las
    //     columnas del usuario para despues tirar casi todas.
    public bool EmailAlreadyUsed(string email)
    {
        return _userRepository.CountByEmail(email) > 0;
    }

    // Reusa GetByUserName en vez de tener un CountByUserName aparte: si el
    // usuario existe, GetByUserName devuelve el User; si no existe, vuelve
    // null. Con eso alcanza para saber si ya esta en uso, y nos ahorramos
    // escribir y mantener una query casi identica en el Repository.
    public bool UserNameAlreadyUsed(string userName)
    {
        User? existingUser = _userRepository.GetByUserName(userName);
        return existingUser != null;
    }
}
