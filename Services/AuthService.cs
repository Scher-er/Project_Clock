using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Serviço de autenticação real (substitui o stub da Fase 1).
///
/// Fluxo:
///   1) Valida campos não-vazios
///   2) Busca o usuário no MySQL via <see cref="IUsuarioDao.BuscarPorLoginAsync"/>
///   3) Verifica que está ATIVO
///   4) Verifica a senha com BCrypt contra <see cref="Usuario.SenhaHash"/>
///   5) Inicia sessão + atualiza UltimoLogin + registra log
///
/// Falhas (usuário inexistente, inativo, senha errada) também são logadas
/// — útil pra auditoria de tentativas de invasão.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUsuarioDao _usuarioDao;
    private readonly ISessaoUsuario _sessao;
    private readonly ILogService _log;

    public string NomeServico => "AuthService";

    public AuthService(IUsuarioDao usuarioDao, ISessaoUsuario sessao, ILogService log)
    {
        _usuarioDao = usuarioDao;
        _sessao = sessao;
        _log = log;
    }

    public async Task<ResultadoOperacao<Usuario>> AutenticarAsync(string login, string senha)
    {
        if (string.IsNullOrWhiteSpace(login))
            return ResultadoOperacao<Usuario>.Falha("Informe o usuário.");
        if (string.IsNullOrWhiteSpace(senha))
            return ResultadoOperacao<Usuario>.Falha("Informe a senha.");

        Usuario? usuario;
        try
        {
            usuario = await _usuarioDao.BuscarPorLoginAsync(login);
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("AUTENTICAR", ex);
            return ResultadoOperacao<Usuario>.Falha(
                "Não foi possível conectar ao banco de dados. Verifique se o MySQL está acessível.");
        }

        if (usuario is null)
        {
            await _log.RegistrarAsync(
                TipoEventoLog.Aviso,
                "TENTATIVA_LOGIN",
                $"Tentativa de login com usuário inexistente: '{login}'");
            return ResultadoOperacao<Usuario>.Falha("Usuário ou senha inválidos.");
        }

        if (!usuario.Ativo)
        {
            await _log.RegistrarAsync(
                TipoEventoLog.Aviso,
                "TENTATIVA_LOGIN",
                $"Tentativa de login de usuário inativo: '{login}'",
                "Usuario", usuario.Id.ToString());
            return ResultadoOperacao<Usuario>.Falha("Usuário inativo. Contate o administrador.");
        }

        bool senhaOk;
        try
        {
            senhaOk = BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash mal formado no banco — provavelmente seed antigo ou dado corrompido
            await _log.RegistrarAsync(
                TipoEventoLog.Erro,
                "AUTENTICAR",
                $"Hash de senha mal formado para usuário '{login}' — reinsira a senha.");
            return ResultadoOperacao<Usuario>.Falha(
                "Erro na configuração do usuário. Contate o administrador.");
        }

        if (!senhaOk)
        {
            await _log.RegistrarAsync(
                TipoEventoLog.Aviso,
                "TENTATIVA_LOGIN",
                $"Senha incorreta para usuário '{login}'",
                "Usuario", usuario.Id.ToString());
            return ResultadoOperacao<Usuario>.Falha("Usuário ou senha inválidos.");
        }

        // ───── Autenticado ─────
        _sessao.IniciarSessao(usuario);
        await _usuarioDao.AtualizarUltimoLoginAsync(usuario.Id);
        await _log.RegistrarAsync(
            TipoEventoLog.Login,
            "AUTENTICOU",
            $"Usuário '{usuario.Login}' autenticou com sucesso.",
            "Usuario", usuario.Id.ToString());

        return ResultadoOperacao<Usuario>.Ok(usuario, "Autenticado com sucesso.");
    }

    public void EncerrarSessao()
    {
        var usuario = _sessao.UsuarioAtual;
        _sessao.EncerrarSessao();

        // Log de logout (fire-and-forget — não precisa esperar)
        if (usuario is not null)
        {
            _ = _log.RegistrarAsync(
                TipoEventoLog.Logout,
                "ENCERROU_SESSAO",
                $"Usuário '{usuario.Login}' encerrou a sessão.",
                "Usuario", usuario.Id.ToString());
        }
    }
}
