import { ApolloClient, InMemoryCache, HttpLink } from "@apollo/client";
import fetch from "cross-fetch";

// Next.js automatically injects NEXT_PUBLIC_* variables at build time
const graphqlUrl = process.env.NEXT_PUBLIC_GRAPHQL_URL || "http://localhost:5148/graphql";

export const apolloServerClient = new ApolloClient({
  link: new HttpLink({
    uri: graphqlUrl,
    fetch,
  }),
  cache: new InMemoryCache(),
  defaultOptions: {
    query: {
      fetchPolicy: "no-cache",
    },
  },
});
