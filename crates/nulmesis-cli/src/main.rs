use std::io::{stderr, stdin, stdout, BufReader};

use anyhow::Result;
use clap::Parser;
use nulmesis_cli::{run_with_io, Cli};

fn main() -> Result<()> {
    let cli = Cli::parse();
    let mut input = BufReader::new(stdin());
    let mut output = stdout();
    let mut error = stderr();
    let code = run_with_io(cli, &mut input, &mut output, &mut error)?;
    std::process::exit(code);
}
